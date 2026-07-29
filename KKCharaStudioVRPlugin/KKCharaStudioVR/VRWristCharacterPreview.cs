using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private const float CharacterPreviewAreaWidth = 250f;
    private const float CharacterPreviewAreaHeight = 326f;

    private RawImage _characterPreviewImage;
    private RectTransform _characterPreviewRect;
    private Text _characterPreviewNameText;
    private VRWristMenuButtonTarget _characterPreviewConfirmButton;
    private Texture2D _characterPreviewTexture;
    private string _pendingCharacterCardPath;

    private void BuildCharacterPreviewPage()
    {
        CreateButton(
            "CharacterPreviewBack",
            "<",
            18f,
            10f,
            new Color(1f, 1f, 1f, 0.08f),
            new Color(1f, 1f, 1f, 0.2f),
            HandleCharacterPreviewBack,
            _characterPreviewPage.transform,
            58f,
            38f,
            28);
        CreateText(
            "CharacterPreviewTitle",
            _characterPreviewPage.transform,
            "角色卡预览",
            92f,
            10f,
            444f,
            40f,
            28,
            TextAnchor.MiddleLeft,
            Color.white);

        Image previewBackground = CreateImage(
            "CharacterPreviewBackground",
            _characterPreviewPage.transform,
            24f,
            70f,
            CharacterPreviewAreaWidth,
            CharacterPreviewAreaHeight,
            new Color(0.02f, 0.03f, 0.035f, 0.72f));
        ApplyGlassEffects(
            previewBackground,
            new Color(0.82f, 0.96f, 1f, 0.16f),
            true,
            2f);

        GameObject previewObject = CreateRectObject(
            "CharacterPreviewImage",
            _characterPreviewPage.transform,
            24f,
            70f,
            CharacterPreviewAreaWidth,
            CharacterPreviewAreaHeight,
            _visibleLayer);
        _characterPreviewRect = previewObject.GetComponent<RectTransform>();
        _characterPreviewImage = previewObject.AddComponent<RawImage>();
        _characterPreviewImage.color = Color.white;
        _characterPreviewImage.raycastTarget = false;

        _characterPreviewNameText = CreateText(
            "CharacterPreviewName",
            _characterPreviewPage.transform,
            "尚未选择角色卡",
            298f,
            76f,
            238f,
            70f,
            18,
            TextAnchor.MiddleLeft,
            new Color(0.72f, 0.9f, 0.96f, 1f));

        _characterPreviewConfirmButton = CreateButton(
            "CharacterPreviewConfirm",
            "确认\nCONFIRM",
            298f,
            168f,
            new Color(0.075f, 0.24f, 0.14f, 0.5f),
            new Color(0.11f, 0.46f, 0.24f, 0.78f),
            HandleCharacterPreviewConfirm,
            _characterPreviewPage.transform,
            238f,
            82f,
            21);
        CreateButton(
            "CharacterPreviewReturn",
            "返回卡片列表\nBACK TO CARDS",
            298f,
            272f,
            new Color(0.08f, 0.15f, 0.18f, 0.5f),
            new Color(0.12f, 0.32f, 0.39f, 0.76f),
            HandleCharacterPreviewBack,
            _characterPreviewPage.transform,
            238f,
            64f,
            18);
    }

    private void OpenCharacterCardPreview(string path)
    {
        if (_operationInProgress)
            return;
        StartCoroutine(LoadCharacterCardPreviewAfterFrame(path));
    }

    private IEnumerator LoadCharacterCardPreviewAfterFrame(string path)
    {
        _operationInProgress = true;
        SetStatus(
            "正在读取卡面预览…",
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
        yield return null;

        try
        {
            if (string.IsNullOrEmpty(path)
                || !File.Exists(path)
                || !string.Equals(
                    Path.GetExtension(path),
                    ".png",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("角色卡文件不存在或格式无效");
            }

            byte[] bytes = ReadPngPreviewBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Destroy(texture);
                throw new InvalidDataException("无法解码角色卡预览图");
            }

            ReleaseCharacterPreviewTexture();
            _characterPreviewTexture = texture;
            _characterPreviewTexture.name = "KKVR_CharacterCardPreview";
            _characterPreviewTexture.filterMode = FilterMode.Bilinear;
            _characterPreviewTexture.wrapMode = TextureWrapMode.Clamp;
            _characterPreviewImage.texture = _characterPreviewTexture;
            FitCharacterPreview(texture.width, texture.height);

            _pendingCharacterCardPath = path;
            _characterPreviewNameText.text = Path.GetFileNameWithoutExtension(path);
            _characterPreviewConfirmButton.SetLabel(GetCharacterPreviewConfirmLabel());
            ShowPage(WristMenuPage.CharacterPreview);
            SetStatus(
                "确认卡面后再执行角色操作",
                new Color(0.35f, 1f, 0.62f, 1f),
                0f);
        }
        catch (Exception ex)
        {
            SetStatus(
                "卡面预览失败：" + ex.Message,
                new Color(1f, 0.38f, 0.34f, 1f),
                8f);
        }
        finally
        {
            _operationInProgress = false;
        }
    }

    private void FitCharacterPreview(int textureWidth, int textureHeight)
    {
        if (_characterPreviewRect == null || textureWidth <= 0 || textureHeight <= 0)
            return;

        float scale = Mathf.Min(
            CharacterPreviewAreaWidth / textureWidth,
            CharacterPreviewAreaHeight / textureHeight);
        float width = textureWidth * scale;
        float height = textureHeight * scale;
        _characterPreviewRect.anchoredPosition = new Vector2(
            24f + (CharacterPreviewAreaWidth - width) * 0.5f,
            -(70f + (CharacterPreviewAreaHeight - height) * 0.5f));
        _characterPreviewRect.sizeDelta = new Vector2(width, height);
    }

    private static byte[] ReadPngPreviewBytes(string path)
    {
        using FileStream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        long pngSize = PngFile.GetPngSize(stream);
        if (pngSize <= 0 || pngSize > int.MaxValue || pngSize > stream.Length)
            throw new InvalidDataException("角色卡没有有效的 PNG 卡面");

        byte[] bytes = new byte[(int)pngSize];
        stream.Position = 0;
        int offset = 0;
        while (offset < bytes.Length)
        {
            int read = stream.Read(bytes, offset, bytes.Length - offset);
            if (read <= 0)
                throw new EndOfStreamException("角色卡 PNG 数据不完整");
            offset += read;
        }
        return bytes;
    }

    private string GetCharacterPreviewConfirmLabel()
    {
        switch (_characterCardMode)
        {
            case VRCharacterCardMode.AddFemale:
                return "确认添加女角色\nADD FEMALE";
            case VRCharacterCardMode.AddMale:
                return "确认添加男角色\nADD MALE";
            default:
                return "确认替换角色\nREPLACE";
        }
    }

    private void HandleCharacterPreviewConfirm()
    {
        if (string.IsNullOrEmpty(_pendingCharacterCardPath))
        {
            SetStatus(
                "尚未选择角色卡",
                new Color(1f, 0.38f, 0.34f, 1f),
                5f);
            return;
        }
        StartCoroutine(ExecuteCharacterCardAfterFrame(_pendingCharacterCardPath));
    }

    private void HandleCharacterPreviewBack()
    {
        ShowPage(WristMenuPage.Browser);
        SetStatus(
            "选择角色卡可展开卡面预览",
            new Color(0.47f, 0.9f, 0.55f, 1f),
            0f);
    }

    private void ReleaseCharacterPreviewTexture()
    {
        if (_characterPreviewImage != null)
            _characterPreviewImage.texture = null;
        if (_characterPreviewTexture != null)
        {
            Destroy(_characterPreviewTexture);
            _characterPreviewTexture = null;
        }
    }
}
