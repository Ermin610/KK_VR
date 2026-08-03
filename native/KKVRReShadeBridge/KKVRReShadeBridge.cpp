// A tiny native bridge between the managed KK VR wrist menu and ReShade's
// official add-on API. Requests arrive on Unity's main thread and are applied
// from each ReShade runtime's own reshade_present callback.

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <Psapi.h>

#include <algorithm>
#include <cstdint>
#include <mutex>
#include <string>
#include <vector>

#include "third_party/reshade/reshade_api.hpp"

namespace
{
    constexpr uint32_t kReShadeApiVersion = 18; // ReShade 6.7.1 SDK
    constexpr uint32_t kEventInitEffectRuntime = 9;
    constexpr uint32_t kEventDestroyEffectRuntime = 10;
    constexpr uint32_t kEventReShadePresent = 75;
    constexpr uint32_t kEventReShadeReloadedEffects = 78;
    constexpr uint32_t kEventSetCurrentPresetPath = 84;
    constexpr uint32_t kEventSetEffectsState = 94;

    using effect_runtime = reshade::api::effect_runtime;
    using register_addon_fn = bool (*)(void *, uint32_t);
    using unregister_addon_fn = void (*)(void *);
    using register_event_fn = void (*)(uint32_t, void *);
    using unregister_event_fn = void (*)(uint32_t, void *);

    struct RuntimeState
    {
        effect_runtime *runtime = nullptr;
        bool is_vr = false;
        bool effects_known = false;
        bool effects_enabled = false;
        std::string preset_path;
        uint64_t applied_effects_generation = 0;
        uint64_t applied_preset_generation = 0;
        uint64_t preset_in_flight_generation = 0;
        uint64_t present_serial = 0;
        uint64_t preset_request_present_serial = 0;
        bool preset_reload_in_progress = false;
        ULONGLONG preset_request_tick = 0;
        ULONGLONG last_effects_attempt_tick = 0;
        ULONGLONG last_present_tick = 0;
    };

    HMODULE g_module = nullptr;
    HMODULE g_reshade_module = nullptr;
    register_addon_fn g_register_addon = nullptr;
    unregister_addon_fn g_unregister_addon = nullptr;
    register_event_fn g_register_event = nullptr;
    unregister_event_fn g_unregister_event = nullptr;

    std::mutex g_mutex;
    std::vector<RuntimeState> g_runtimes;
    bool g_registered = false;
    bool g_desired_effects_known = false;
    bool g_desired_effects_enabled = false;
    std::string g_desired_preset_path;
    uint64_t g_effects_generation = 0;
    uint64_t g_preset_generation = 0;

    RuntimeState *find_runtime_unlocked(effect_runtime *runtime)
    {
        const auto it = std::find_if(
            g_runtimes.begin(),
            g_runtimes.end(),
            [runtime](const RuntimeState &state) { return state.runtime == runtime; });
        return it == g_runtimes.end() ? nullptr : &*it;
    }

    bool is_runtime_active_unlocked(const RuntimeState &state, ULONGLONG now)
    {
        return state.last_present_tick != 0
            && now - state.last_present_tick <= 2000;
    }

    bool has_active_runtime_unlocked(ULONGLONG now)
    {
        return std::find_if(
                   g_runtimes.cbegin(),
                   g_runtimes.cend(),
                   [now](const RuntimeState &state) {
                       return is_runtime_active_unlocked(state, now);
                   }) != g_runtimes.cend();
    }

    std::string query_preset_path(effect_runtime *runtime)
    {
        if (runtime == nullptr)
            return {};

        size_t size = 0;
        runtime->get_current_preset_path(nullptr, &size);
        if (size <= 1 || size > 32768)
            return {};

        std::vector<char> buffer(size, '\0');
        runtime->get_current_preset_path(buffer.data(), &size);
        return buffer.data();
    }

    std::wstring utf8_to_wide(const std::string &value)
    {
        if (value.empty())
            return {};

        const int count = MultiByteToWideChar(
            CP_UTF8, MB_ERR_INVALID_CHARS, value.c_str(), -1, nullptr, 0);
        if (count <= 1)
            return {};

        std::wstring result(static_cast<size_t>(count), L'\0');
        MultiByteToWideChar(
            CP_UTF8, MB_ERR_INVALID_CHARS, value.c_str(), -1, &result[0], count);
        result.resize(static_cast<size_t>(count - 1));
        return result;
    }

    std::string wide_to_utf8(const wchar_t *value)
    {
        if (value == nullptr || value[0] == L'\0')
            return {};

        const int count = WideCharToMultiByte(
            CP_UTF8, WC_ERR_INVALID_CHARS, value, -1, nullptr, 0, nullptr, nullptr);
        if (count <= 1)
            return {};

        std::string result(static_cast<size_t>(count), '\0');
        WideCharToMultiByte(
            CP_UTF8, WC_ERR_INVALID_CHARS, value, -1, &result[0], count, nullptr, nullptr);
        result.resize(static_cast<size_t>(count - 1));
        return result;
    }

    bool is_reshade_module(HMODULE module)
    {
        return module != nullptr
            && GetProcAddress(module, "ReShadeRegisterAddon") != nullptr
            && GetProcAddress(module, "ReShadeRegisterEvent") != nullptr;
    }

    HMODULE find_reshade_module()
    {
        HMODULE modules[1024] = {};
        DWORD needed = 0;
        if (!K32EnumProcessModules(GetCurrentProcess(), modules, sizeof(modules), &needed))
            return nullptr;

        const DWORD count = (std::min)(needed, static_cast<DWORD>(sizeof(modules))) / sizeof(HMODULE);
        for (DWORD i = 0; i < count; ++i)
        {
            if (is_reshade_module(modules[i]))
                return modules[i];
        }
        return nullptr;
    }

    void on_init_effect_runtime(effect_runtime *runtime)
    {
        if (runtime == nullptr)
            return;

        RuntimeState state;
        state.runtime = runtime;
        state.is_vr = runtime->get_hwnd() == nullptr;
        state.effects_known = true;
        state.effects_enabled = runtime->get_effects_state();
        state.preset_path = query_preset_path(runtime);

        const std::lock_guard<std::mutex> lock(g_mutex);
        if (find_runtime_unlocked(runtime) == nullptr)
            g_runtimes.push_back(std::move(state));
    }

    void on_destroy_effect_runtime(effect_runtime *runtime)
    {
        const std::lock_guard<std::mutex> lock(g_mutex);
        g_runtimes.erase(
            std::remove_if(
                g_runtimes.begin(),
                g_runtimes.end(),
                [runtime](const RuntimeState &state) { return state.runtime == runtime; }),
            g_runtimes.end());
    }

    bool on_set_effects_state(effect_runtime *runtime, bool enabled)
    {
        (void)enabled;
        const std::lock_guard<std::mutex> lock(g_mutex);
        if (RuntimeState *state = find_runtime_unlocked(runtime))
        {
            // This event is raised before the state is committed and another add-on
            // may veto it. Force a read-back from the next present callback.
            state->effects_known = false;
        }
        return false;
    }

    void on_reshade_reloaded_effects(effect_runtime *runtime)
    {
        const std::lock_guard<std::mutex> lock(g_mutex);
        if (RuntimeState *state = find_runtime_unlocked(runtime))
        {
            // ReShade 6.7.1 raises this event at the beginning of a reload.
            // Keep the request pending until event 84 confirms that the preset
            // was loaded and applied after compilation finished.
            if (state->preset_in_flight_generation == g_preset_generation
                && state->preset_in_flight_generation != 0)
            {
                state->preset_reload_in_progress = true;
            }
        }
    }

    void on_set_current_preset_path(effect_runtime *runtime, const char *path)
    {
        const std::lock_guard<std::mutex> lock(g_mutex);
        if (RuntimeState *state = find_runtime_unlocked(runtime))
        {
            state->preset_path = path != nullptr ? path : "";
            if (!g_desired_preset_path.empty()
                && _stricmp(state->preset_path.c_str(), g_desired_preset_path.c_str()) == 0)
            {
                state->applied_preset_generation = g_preset_generation;
                state->preset_in_flight_generation = 0;
                state->preset_request_present_serial = 0;
                state->preset_reload_in_progress = false;
                state->preset_request_tick = 0;
            }
        }
    }

    void on_reshade_present(effect_runtime *runtime)
    {
        const ULONGLONG now = GetTickCount64();
        const bool actual_effects_enabled = runtime->get_effects_state();
        const std::string actual_preset_path = query_preset_path(runtime);
        bool apply_effects = false;
        bool effects_enabled = false;
        uint64_t effects_generation = 0;
        bool apply_preset = false;
        std::string preset_path;
        uint64_t preset_generation = 0;

        {
            const std::lock_guard<std::mutex> lock(g_mutex);
            RuntimeState *state = find_runtime_unlocked(runtime);
            if (state == nullptr)
            {
                // ReShade may load an add-on after the runtime initialization
                // event has already fired. The per-frame callback is authoritative,
                // so discover that runtime lazily instead of waiting forever.
                RuntimeState discovered;
                discovered.runtime = runtime;
                discovered.is_vr = runtime->get_hwnd() == nullptr;
                discovered.effects_known = true;
                discovered.effects_enabled = actual_effects_enabled;
                discovered.preset_path = actual_preset_path;
                g_runtimes.push_back(std::move(discovered));
                state = &g_runtimes.back();
            }

            state->last_present_tick = now;
            ++state->present_serial;
            state->effects_known = true;
            state->effects_enabled = actual_effects_enabled;
            state->preset_path = actual_preset_path;
            if (g_desired_effects_known
                && actual_effects_enabled == g_desired_effects_enabled)
            {
                state->applied_effects_generation = g_effects_generation;
            }

            if (g_preset_generation != 0
                && state->applied_preset_generation < g_preset_generation)
            {
                const bool path_confirmed = !g_desired_preset_path.empty()
                    && _stricmp(state->preset_path.c_str(), g_desired_preset_path.c_str()) == 0;
                if (state->preset_in_flight_generation == 0 && path_confirmed)
                {
                    state->applied_preset_generation = g_preset_generation;
                }
                else if (state->preset_in_flight_generation == g_preset_generation
                    && !state->preset_reload_in_progress
                    && state->present_serial > state->preset_request_present_serial
                    && path_confirmed)
                {
                    // The programmatic setter does not emit event 84 when no
                    // shader reload is needed. Confirm it by reading the path
                    // back on the following rendered frame instead.
                    state->applied_preset_generation = g_preset_generation;
                    state->preset_in_flight_generation = 0;
                    state->preset_request_present_serial = 0;
                    state->preset_request_tick = 0;
                }

                const bool request_timed_out = state->preset_in_flight_generation == g_preset_generation
                    && now - state->preset_request_tick >= 15000;
                if (request_timed_out)
                {
                    state->preset_in_flight_generation = 0;
                    state->preset_request_present_serial = 0;
                    state->preset_reload_in_progress = false;
                    state->preset_request_tick = 0;
                }

                if (state->applied_preset_generation < g_preset_generation
                    && state->preset_in_flight_generation != g_preset_generation)
                {
                    apply_preset = !g_desired_preset_path.empty();
                    preset_path = g_desired_preset_path;
                    if (apply_preset)
                    {
                        // Mark the request in flight before calling ReShade.
                        // Reload event 78 can be raised synchronously by the
                        // setter and therefore must be able to see this state.
                        state->preset_in_flight_generation = g_preset_generation;
                        state->preset_request_present_serial = state->present_serial;
                        state->preset_reload_in_progress = false;
                        state->preset_request_tick = now;
                    }
                }
                preset_generation = g_preset_generation;
            }

            if (g_desired_effects_known
                && state->applied_effects_generation < g_effects_generation)
            {
                if (!state->effects_known || state->effects_enabled != g_desired_effects_enabled)
                {
                    if (now - state->last_effects_attempt_tick >= 250)
                    {
                        apply_effects = true;
                        effects_enabled = g_desired_effects_enabled;
                        state->last_effects_attempt_tick = now;
                    }
                }
                else
                {
                    state->applied_effects_generation = g_effects_generation;
                }
                effects_generation = g_effects_generation;
            }
        }

        // ReShade's runtime object is only touched from its own present callback.
        if (apply_preset)
            runtime->set_current_preset_path(preset_path.c_str());
        if (apply_effects)
            runtime->set_effects_state(effects_enabled);

        if (apply_preset || apply_effects)
        {
            const bool confirmed_effects = runtime->get_effects_state();
            const std::string confirmed_path = query_preset_path(runtime);
            const std::lock_guard<std::mutex> lock(g_mutex);
            if (RuntimeState *state = find_runtime_unlocked(runtime))
            {
                if (apply_preset && preset_generation <= g_preset_generation)
                {
                    state->preset_path = confirmed_path;
                }
                if (apply_effects && effects_generation <= g_effects_generation)
                {
                    state->effects_known = true;
                    state->effects_enabled = confirmed_effects;
                    if (confirmed_effects == effects_enabled)
                        state->applied_effects_generation = effects_generation;
                }
            }
        }
    }

    bool register_bridge(HMODULE module)
    {
        g_reshade_module = find_reshade_module();
        if (g_reshade_module == nullptr)
            return false;

        g_register_addon = reinterpret_cast<register_addon_fn>(
            GetProcAddress(g_reshade_module, "ReShadeRegisterAddon"));
        g_unregister_addon = reinterpret_cast<unregister_addon_fn>(
            GetProcAddress(g_reshade_module, "ReShadeUnregisterAddon"));
        g_register_event = reinterpret_cast<register_event_fn>(
            GetProcAddress(g_reshade_module, "ReShadeRegisterEvent"));
        g_unregister_event = reinterpret_cast<unregister_event_fn>(
            GetProcAddress(g_reshade_module, "ReShadeUnregisterEvent"));

        if (g_register_addon == nullptr
            || g_unregister_addon == nullptr
            || g_register_event == nullptr
            || g_unregister_event == nullptr
            || !g_register_addon(module, kReShadeApiVersion))
        {
            return false;
        }

        g_register_event(kEventInitEffectRuntime, reinterpret_cast<void *>(&on_init_effect_runtime));
        g_register_event(kEventDestroyEffectRuntime, reinterpret_cast<void *>(&on_destroy_effect_runtime));
        g_register_event(kEventReShadePresent, reinterpret_cast<void *>(&on_reshade_present));
        g_register_event(kEventReShadeReloadedEffects, reinterpret_cast<void *>(&on_reshade_reloaded_effects));
        g_register_event(kEventSetEffectsState, reinterpret_cast<void *>(&on_set_effects_state));
        g_register_event(kEventSetCurrentPresetPath, reinterpret_cast<void *>(&on_set_current_preset_path));
        return true;
    }

    void unregister_bridge()
    {
        if (!g_registered || g_unregister_event == nullptr || g_unregister_addon == nullptr)
            return;

        g_unregister_event(kEventSetCurrentPresetPath, reinterpret_cast<void *>(&on_set_current_preset_path));
        g_unregister_event(kEventSetEffectsState, reinterpret_cast<void *>(&on_set_effects_state));
        g_unregister_event(kEventReShadeReloadedEffects, reinterpret_cast<void *>(&on_reshade_reloaded_effects));
        g_unregister_event(kEventReShadePresent, reinterpret_cast<void *>(&on_reshade_present));
        g_unregister_event(kEventDestroyEffectRuntime, reinterpret_cast<void *>(&on_destroy_effect_runtime));
        g_unregister_event(kEventInitEffectRuntime, reinterpret_cast<void *>(&on_init_effect_runtime));
        g_unregister_addon(g_module);
        g_registered = false;
    }
}

extern "C" __declspec(dllexport) const char *NAME = "KK VR ReShade Bridge";
extern "C" __declspec(dllexport) const char *AUTHOR = "Ermin";
extern "C" __declspec(dllexport) const char *DESCRIPTION =
    "Lets the KK VR wrist menu safely control ReShade effect runtimes and presets.";

extern "C" __declspec(dllexport) int KKVR_ReShade_GetBridgeVersion()
{
    return 3;
}

extern "C" __declspec(dllexport) int KKVR_ReShade_RequestEffects(int enabled)
{
    if (!g_registered)
        return 0;

    const std::lock_guard<std::mutex> lock(g_mutex);
    g_desired_effects_known = true;
    g_desired_effects_enabled = enabled != 0;
    ++g_effects_generation;
    return has_active_runtime_unlocked(GetTickCount64()) ? 1 : 2;
}

extern "C" __declspec(dllexport) int KKVR_ReShade_RequestPreset(
    const wchar_t *preset_path,
    int enable_effects)
{
    if (!g_registered)
        return 0;

    const std::string utf8_path = wide_to_utf8(preset_path);
    if (utf8_path.empty())
        return -1;

    const DWORD attributes = GetFileAttributesW(preset_path);
    if (attributes == INVALID_FILE_ATTRIBUTES || (attributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
        return -2;

    const std::lock_guard<std::mutex> lock(g_mutex);
    g_desired_preset_path = utf8_path;
    ++g_preset_generation;
    g_desired_effects_known = true;
    g_desired_effects_enabled = enable_effects != 0;
    ++g_effects_generation;
    return has_active_runtime_unlocked(GetTickCount64()) ? 1 : 2;
}

// effects_state: -1 unknown/no runtime, 0 disabled, 1 enabled, 2 mixed.
// preset_state: -1 unknown/no runtime, 0 synchronized, 1 mixed.
extern "C" __declspec(dllexport) int KKVR_ReShade_GetSnapshot(
    int *runtime_count,
    int *vr_runtime_count,
    int *effects_state,
    int *preset_state,
    int *request_pending,
    wchar_t *preset_path,
    int preset_path_capacity)
{
    if (!g_registered)
        return 0;

    std::wstring wide_path;
    int aggregate_effects = -1;
    int aggregate_preset = -1;
    int pending = 0;

    {
        const std::lock_guard<std::mutex> lock(g_mutex);
        const ULONGLONG now = GetTickCount64();
        const int active_runtime_count = static_cast<int>(std::count_if(
            g_runtimes.cbegin(),
            g_runtimes.cend(),
            [now](const RuntimeState &state) {
                return is_runtime_active_unlocked(state, now);
            }));
        if (runtime_count != nullptr)
            *runtime_count = active_runtime_count;
        const int active_vr_runtime_count = static_cast<int>(std::count_if(
            g_runtimes.cbegin(),
            g_runtimes.cend(),
            [now](const RuntimeState &state) {
                return state.is_vr && is_runtime_active_unlocked(state, now);
            }));
        if (vr_runtime_count != nullptr)
            *vr_runtime_count = active_vr_runtime_count;

        bool first_effect_state = true;
        bool first_effect_enabled = false;
        std::string first_path;
        bool mixed_path = false;

        for (const RuntimeState &state : g_runtimes)
        {
            const bool active = is_runtime_active_unlocked(state, now);
            if (!active)
                continue;

            if (state.effects_known)
            {
                if (first_effect_state)
                {
                    first_effect_state = false;
                    first_effect_enabled = state.effects_enabled;
                    aggregate_effects = state.effects_enabled ? 1 : 0;
                }
                else if (state.effects_enabled != first_effect_enabled)
                {
                    aggregate_effects = 2;
                }
            }

            if (first_path.empty() && !state.preset_path.empty())
                first_path = state.preset_path;
            else if (!first_path.empty()
                && !state.preset_path.empty()
                && _stricmp(first_path.c_str(), state.preset_path.c_str()) != 0)
                mixed_path = true;

            // Inactive runtimes are ignored for completion. They retain the
            // generation and will apply it if they start presenting again.
            if ((g_effects_generation != 0
                        && state.applied_effects_generation < g_effects_generation)
                    || (g_preset_generation != 0
                        && state.applied_preset_generation < g_preset_generation))
            {
                pending = 1;
            }
        }

        aggregate_preset = first_path.empty() ? -1 : (mixed_path ? 1 : 0);

        const std::string &reported_path =
            (!g_desired_preset_path.empty() && (pending != 0 || mixed_path))
                ? g_desired_preset_path
                : first_path;
        wide_path = utf8_to_wide(reported_path);
    }

    if (effects_state != nullptr)
        *effects_state = aggregate_effects;
    if (preset_state != nullptr)
        *preset_state = aggregate_preset;
    if (request_pending != nullptr)
        *request_pending = pending;
    if (preset_path != nullptr && preset_path_capacity > 0)
    {
        wcsncpy_s(
            preset_path,
            static_cast<size_t>(preset_path_capacity),
            wide_path.c_str(),
            _TRUNCATE);
    }
    return 1;
}

BOOL APIENTRY DllMain(HMODULE module, DWORD reason, LPVOID)
{
    switch (reason)
    {
    case DLL_PROCESS_ATTACH:
        g_module = module;
        DisableThreadLibraryCalls(module);
        g_registered = register_bridge(module);
        return g_registered ? TRUE : FALSE;
    case DLL_PROCESS_DETACH:
        unregister_bridge();
        break;
    }
    return TRUE;
}
