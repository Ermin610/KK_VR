using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Manager;
using Studio;
using UnityEngine;

namespace KKCharaStudioVR;

internal sealed class VRTrackedAccessorySlotInfo
{
    public int SlotIndex;
    public string DisplayName;
}

internal sealed class VRTrackedAccessorySnapshot
{
    internal readonly List<VRTrackedAccessoryRecord> Records =
        new List<VRTrackedAccessoryRecord>();
}

internal sealed class VRTrackedAccessoryRecord
{
    public int SlotIndex;
    public byte[] Fingerprint;
    public string DisplayName;

    public VRTrackedAccessoryRecord Clone()
    {
        return new VRTrackedAccessoryRecord
        {
            SlotIndex = SlotIndex,
            Fingerprint = Fingerprint != null ? (byte[])Fingerprint.Clone() : null,
            DisplayName = DisplayName
        };
    }
}

internal sealed class VRTrackedAccessoryCharacterEntry
{
    public readonly WeakReference Character;
    public List<VRTrackedAccessoryRecord> Records;

    public VRTrackedAccessoryCharacterEntry(
        OCIChar character,
        List<VRTrackedAccessoryRecord> records)
    {
        Character = new WeakReference(character);
        Records = records;
    }
}

/// <summary>
/// Tracks only target slots that changed from empty to occupied during this
/// plugin's append-only coordinate loads. Slot indices are never compacted, so
/// MoreAccessories 2.x arrays keep their original layout.
/// </summary>
internal static class VRCoordinateAccessoryTracker
{
    private static readonly object SyncRoot = new object();
    private static readonly List<VRTrackedAccessoryCharacterEntry> CharacterEntries =
        new List<VRTrackedAccessoryCharacterEntry>();

    public static void ClearAll()
    {
        lock (SyncRoot)
            CharacterEntries.Clear();
    }

    public static void Clear(OCIChar character)
    {
        if (character == null)
            return;

        lock (SyncRoot)
            RemoveCharacterLocked(character);
    }

    public static List<VRTrackedAccessorySlotInfo> GetValidSlots(OCIChar character)
    {
        lock (SyncRoot)
        {
            List<VRTrackedAccessoryRecord> records = GetValidRecordsLocked(character);
            return records
                .OrderBy(record => record.SlotIndex)
                .Select(record => new VRTrackedAccessorySlotInfo
                {
                    SlotIndex = record.SlotIndex,
                    DisplayName = record.DisplayName
                })
                .ToList();
        }
    }

    public static int[] FilterDuplicateSourceSlots(
        OCIChar character,
        ChaFileCoordinate source,
        IEnumerable<int> sourceSlots,
        out int skippedCount)
    {
        skippedCount = 0;
        int[] normalized = (sourceSlots ?? Enumerable.Empty<int>())
            .Where(slot => slot >= 0)
            .Distinct()
            .OrderBy(slot => slot)
            .ToArray();
        if (normalized.Length == 0)
            return normalized;

        lock (SyncRoot)
        {
            // Deduplicate against every currently occupied target slot, not only
            // against this plugin's volatile deletion registry. This keeps exact
            // duplicate suppression working after a plugin/game restart while the
            // registry remains deliberately narrower for safe deletion.
            ChaFileAccessory.PartsInfo[] targetParts =
                character?.charInfo?.nowCoordinate?.accessory?.parts;
            List<byte[]> availableFingerprints = targetParts == null
                ? new List<byte[]>()
                : targetParts
                    .Where(IsOccupied)
                    .Select(CreateFingerprint)
                    .ToList();
            if (availableFingerprints.Count == 0)
                return normalized;

            ChaFileAccessory.PartsInfo[] parts = source?.accessory?.parts;
            List<int> filtered = new List<int>(normalized.Length);
            foreach (int slot in normalized)
            {
                // Legacy MoreAccessories stores are not represented in parts[].
                // Keep those selections; current MoreAccessories 2.x expands this
                // array and therefore receives exact fingerprint deduplication.
                if (parts == null
                    || slot >= parts.Length
                    || !IsOccupied(parts[slot]))
                {
                    filtered.Add(slot);
                    continue;
                }

                byte[] fingerprint = CreateFingerprint(parts[slot]);
                int duplicateIndex = availableFingerprints.FindIndex(
                    candidate => FingerprintsEqual(candidate, fingerprint));
                if (duplicateIndex < 0)
                {
                    filtered.Add(slot);
                    continue;
                }

                // Treat duplicates as a multiset. Two identical items in one card
                // are both retained unless two identical target items already exist.
                availableFingerprints.RemoveAt(duplicateIndex);
                skippedCount++;
            }
            return filtered.ToArray();
        }
    }

    public static byte[][] CaptureSlotFingerprints(ChaFileAccessory accessory)
    {
        ChaFileAccessory.PartsInfo[] parts = accessory?.parts;
        if (parts == null)
            return new byte[0][];

        byte[][] fingerprints = new byte[parts.Length][];
        for (int i = 0; i < parts.Length; i++)
        {
            if (IsOccupied(parts[i]))
                fingerprints[i] = CreateFingerprint(parts[i]);
        }
        return fingerprints;
    }

    public static int RegisterNewlyAppendedSlots(OCIChar character, byte[][] before)
    {
        lock (SyncRoot)
        {
            ChaFileAccessory.PartsInfo[] current =
                character?.charInfo?.nowCoordinate?.accessory?.parts;
            if (current == null)
                return 0;

            List<VRTrackedAccessoryRecord> records = GetValidRecordsLocked(character);
            int added = 0;
            for (int slot = 0; slot < current.Length; slot++)
            {
                if (!IsOccupied(current[slot]))
                    continue;
                if (before != null
                    && slot < before.Length
                    && before[slot] != null)
                {
                    continue;
                }

                byte[] fingerprint = CreateFingerprint(current[slot]);
                int existingIndex = records.FindIndex(record => record.SlotIndex == slot);
                VRTrackedAccessoryRecord record = new VRTrackedAccessoryRecord
                {
                    SlotIndex = slot,
                    Fingerprint = fingerprint,
                    DisplayName = ResolveDisplayName(current[slot])
                };
                if (existingIndex >= 0)
                    records[existingIndex] = record;
                else
                    records.Add(record);
                added++;
            }

            if (records.Count > 0)
                SetRecordsLocked(character, records);
            return added;
        }
    }

    public static VRTrackedAccessorySnapshot Capture(OCIChar character)
    {
        lock (SyncRoot)
        {
            VRTrackedAccessorySnapshot snapshot = new VRTrackedAccessorySnapshot();
            foreach (VRTrackedAccessoryRecord record in GetValidRecordsLocked(character))
                snapshot.Records.Add(record.Clone());
            return snapshot;
        }
    }

    public static void Restore(OCIChar character, VRTrackedAccessorySnapshot snapshot)
    {
        lock (SyncRoot)
        {
            if (character == null || snapshot == null || snapshot.Records.Count == 0)
            {
                if (character != null)
                    RemoveCharacterLocked(character);
                return;
            }

            SetRecordsLocked(
                character,
                snapshot.Records.Select(record => record.Clone()).ToList());
        }
    }

    public static int[] ResolveValidRemovalSlots(
        OCIChar character,
        IEnumerable<int> requestedSlots)
    {
        lock (SyncRoot)
        {
            HashSet<int> requested = new HashSet<int>(
                (requestedSlots ?? Enumerable.Empty<int>()).Where(slot => slot >= 0));
            if (requested.Count == 0)
                return new int[0];

            return GetValidRecordsLocked(character)
                .Where(record => requested.Contains(record.SlotIndex))
                .Select(record => record.SlotIndex)
                .Distinct()
                .OrderBy(slot => slot)
                .ToArray();
        }
    }

    public static int ApplyRemoval(OCIChar character, IEnumerable<int> requestedSlots)
    {
        lock (SyncRoot)
        {
            int[] requested = (requestedSlots ?? Enumerable.Empty<int>())
                .Where(slot => slot >= 0)
                .Distinct()
                .OrderBy(slot => slot)
                .ToArray();
            int[] slots = ResolveValidRemovalSlots(character, requested);
            ChaFileAccessory.PartsInfo[] parts =
                character?.charInfo?.nowCoordinate?.accessory?.parts;
            if (parts == null || slots.Length == 0 || slots.Length != requested.Length)
                return 0;

            // Revalidate every slot before mutating any of them. A user edit that
            // changes the full fingerprint makes the whole request fail safely.
            if (slots.Any(slot => slot >= parts.Length || !IsTrackedSlotValidLocked(character, slot)))
                return 0;

            foreach (int slot in slots)
                parts[slot] = new ChaFileAccessory.PartsInfo();
            return slots.Length;
        }
    }

    public static void CommitRemoval(OCIChar character, IEnumerable<int> removedSlots)
    {
        lock (SyncRoot)
        {
            VRTrackedAccessoryCharacterEntry entry = FindCharacterEntryLocked(character);
            if (entry == null)
                return;

            List<VRTrackedAccessoryRecord> records = entry.Records;
            HashSet<int> removed = new HashSet<int>(removedSlots ?? Enumerable.Empty<int>());
            records.RemoveAll(record => removed.Contains(record.SlotIndex));
            if (records.Count == 0)
                CharacterEntries.Remove(entry);
        }
    }

    private static List<VRTrackedAccessoryRecord> GetValidRecordsLocked(OCIChar character)
    {
        if (character == null
            || character.charInfo == null
            || character.charInfo.nowCoordinate?.accessory?.parts == null)
        {
            if (character != null)
                RemoveCharacterLocked(character);
            return new List<VRTrackedAccessoryRecord>();
        }

        VRTrackedAccessoryCharacterEntry entry = FindCharacterEntryLocked(character);
        if (entry == null)
            return new List<VRTrackedAccessoryRecord>();

        List<VRTrackedAccessoryRecord> records = entry.Records;
        ChaFileAccessory.PartsInfo[] parts = character.charInfo.nowCoordinate.accessory.parts;
        records.RemoveAll(record => record == null
            || record.Fingerprint == null
            || record.SlotIndex < 0
            || record.SlotIndex >= parts.Length
            || !IsOccupied(parts[record.SlotIndex])
            || !FingerprintsEqual(record.Fingerprint, CreateFingerprint(parts[record.SlotIndex])));
        if (records.Count == 0)
            CharacterEntries.Remove(entry);
        return records;
    }

    private static VRTrackedAccessoryCharacterEntry FindCharacterEntryLocked(OCIChar character)
    {
        PruneDeadEntriesLocked();
        if (character == null)
            return null;

        foreach (VRTrackedAccessoryCharacterEntry entry in CharacterEntries)
        {
            if (ReferenceEquals(entry.Character.Target, character))
                return entry;
        }
        return null;
    }

    private static void SetRecordsLocked(
        OCIChar character,
        List<VRTrackedAccessoryRecord> records)
    {
        if (character == null)
            return;

        VRTrackedAccessoryCharacterEntry entry = FindCharacterEntryLocked(character);
        if (entry == null)
        {
            CharacterEntries.Add(
                new VRTrackedAccessoryCharacterEntry(character, records));
        }
        else
        {
            entry.Records = records;
        }
    }

    private static void RemoveCharacterLocked(OCIChar character)
    {
        VRTrackedAccessoryCharacterEntry entry = FindCharacterEntryLocked(character);
        if (entry != null)
            CharacterEntries.Remove(entry);
    }

    private static void PruneDeadEntriesLocked()
    {
        CharacterEntries.RemoveAll(entry =>
            entry == null
            || entry.Character == null
            || !entry.Character.IsAlive
            || entry.Character.Target == null);
    }

    private static bool IsTrackedSlotValidLocked(OCIChar character, int slot)
    {
        List<VRTrackedAccessoryRecord> records = GetValidRecordsLocked(character);
        return records.Any(record => record.SlotIndex == slot);
    }

    internal static bool IsOccupied(ChaFileAccessory.PartsInfo part)
    {
        return part != null && part.type != 120;
    }

    private static string ResolveDisplayName(ChaFileAccessory.PartsInfo part)
    {
        try
        {
            string name = Singleton<Manager.Character>.Instance?
                .chaListCtrl?
                .GetListInfo((ChaListDefine.CategoryNo)part.type, part.id)?
                .Name;
            if (!string.IsNullOrEmpty(name) && name != "0")
                return name;
        }
        catch (Exception)
        {
            // Missing mod list entries still remain safely manageable by slot.
        }
        return null;
    }

    /// <summary>
    /// Serializes every persistent PartsInfo value into a deterministic byte
    /// sequence. This deliberately includes transforms and colors, preventing
    /// type/id-only false matches between visually different accessories.
    /// </summary>
    internal static byte[] CreateFingerprint(ChaFileAccessory.PartsInfo part)
    {
        if (!IsOccupied(part))
            return null;

        using MemoryStream stream = new MemoryStream();
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(part.type);
            writer.Write(part.id);
            writer.Write(part.parentKey ?? string.Empty);

            Vector3[,] moves = part.addMove;
            int moveRows = moves?.GetLength(0) ?? 0;
            int moveColumns = moves?.GetLength(1) ?? 0;
            writer.Write(moveRows);
            writer.Write(moveColumns);
            for (int row = 0; row < moveRows; row++)
            {
                for (int column = 0; column < moveColumns; column++)
                {
                    Vector3 value = moves[row, column];
                    writer.Write(value.x);
                    writer.Write(value.y);
                    writer.Write(value.z);
                }
            }

            Color[] colors = part.color;
            writer.Write(colors?.Length ?? 0);
            if (colors != null)
            {
                foreach (Color value in colors)
                {
                    writer.Write(value.r);
                    writer.Write(value.g);
                    writer.Write(value.b);
                    writer.Write(value.a);
                }
            }

            writer.Write(part.hideCategory);
            writer.Write(part.noShake);
            writer.Flush();
        }
        return stream.ToArray();
    }

    internal static bool FingerprintsEqual(byte[] left, byte[] right)
    {
        return left != null && right != null && left.SequenceEqual(right);
    }
}
