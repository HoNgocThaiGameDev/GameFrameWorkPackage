using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [DisallowMultipleComponent]
    public class PlayerElement : MonoBehaviour
    {
        [SerializeField] bool isCurrentPlayer;
        [SerializeField] int rank = -1;
        [SerializeField] string playerName;
        [SerializeField] int score;
        [SerializeField] Image backgroundImage;
        [SerializeField] Color currentPlayerBackgroundColor = new Color(0.2f, 0.85f, 0.25f, 1f);

        private Color defaultBackgroundColor = Color.white;
        private bool defaultBackgroundColorCached;

        public bool IsCurrentPlayer => isCurrentPlayer;
        public int Rank => rank;
        public string PlayerName => playerName;
        public int Score => score;
        public RectTransform RectTransform => (RectTransform)transform;

        public void SetCurrentPlayer(bool value)
        {
            isCurrentPlayer = value;
            ApplyCurrentPlayerBackground();
        }

        public void Apply(LeaderboardEntry entry, Sprite avatar = null, Sprite frame = null)
        {
            if (entry == null)
                return;

            rank = entry.Rank;
            playerName = string.IsNullOrWhiteSpace(entry.PlayerName) ? "Guest" : entry.PlayerName;
            score = entry.Score;
            isCurrentPlayer = entry.IsCurrentPlayer;

            SetChildText("Rank", rank.ToString());
            SetChildText("Player Name", playerName);
            SetScoreText(score.ToString());
            SetChildSprite("Avatar Portrait", avatar);
            SetChildSprite("Avatar Frame", frame);
            ApplyCurrentPlayerBackground();
            gameObject.name = $"Row {rank} - {playerName}";
        }

        public void RefreshCachedData()
        {
            rank = ResolveRank();
            playerName = ResolvePlayerName();
            score = ResolveScore();
        }

        private void Reset()
        {
            RefreshCachedData();
            CacheBackgroundImage();
        }

        private void OnValidate()
        {
            if (rank <= 0 || string.IsNullOrWhiteSpace(playerName))
                RefreshCachedData();

            CacheBackgroundImage();
        }

        private void CacheBackgroundImage()
        {
            if (backgroundImage == null)
            {
                Transform backgroundTransform = transform.Find("Background");
                if (backgroundTransform != null)
                    backgroundImage = backgroundTransform.GetComponent<Image>();
            }

            if (backgroundImage != null && !defaultBackgroundColorCached)
            {
                defaultBackgroundColor = backgroundImage.color;
                defaultBackgroundColorCached = true;
            }
        }

        private void ApplyCurrentPlayerBackground()
        {
            CacheBackgroundImage();
            if (backgroundImage == null)
                return;

            backgroundImage.color = isCurrentPlayer ? currentPlayerBackgroundColor : defaultBackgroundColor;
        }

        private int ResolveRank()
        {
            int rankFromName = ParseRankFromRowName(gameObject.name);
            if (rankFromName > 0)
                return rankFromName;

            TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                int parsedRank = ParseRankFromText(tmpTexts[i].text);
                if (parsedRank > 0)
                    return parsedRank;
            }

            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                int parsedRank = ParseRankFromText(legacyTexts[i].text);
                if (parsedRank > 0)
                    return parsedRank;
            }

            return -1;
        }

        private string ResolvePlayerName()
        {
            TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TMP_Text text = tmpTexts[i];
                if (text != null && text.gameObject.name == "Player Name" && !string.IsNullOrWhiteSpace(text.text))
                    return text.text.Trim();
            }

            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text != null && text.gameObject.name == "Player Name" && !string.IsNullOrWhiteSpace(text.text))
                    return text.text.Trim();
            }

            int separatorIndex = gameObject.name.IndexOf(" - ", System.StringComparison.Ordinal);
            return separatorIndex >= 0 && separatorIndex + 3 < gameObject.name.Length
                ? gameObject.name.Substring(separatorIndex + 3).Trim()
                : gameObject.name;
        }

        private int ResolveScore()
        {
            TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TMP_Text text = tmpTexts[i];
                if (text != null && (text.gameObject.name == "Score Value" || text.gameObject.name == "Score"))
                {
                    int parsedScore = ParseRankFromText(text.text);
                    if (parsedScore >= 0)
                        return parsedScore;
                }
            }

            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text != null && (text.gameObject.name == "Score Value" || text.gameObject.name == "Score"))
                {
                    int parsedScore = ParseRankFromText(text.text);
                    if (parsedScore >= 0)
                        return parsedScore;
                }
            }

            return 0;
        }

        private void SetChildText(string childName, string value)
        {
            TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TMP_Text text = tmpTexts[i];
                if (text != null && text.gameObject.name == childName)
                    text.text = value;
            }

            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text != null && text.gameObject.name == childName)
                    text.text = value;
            }
        }

        private void SetScoreText(string value)
        {
            Color scoreColor = ResolvePlayerNameColor();
            TMP_Text keptTmpScoreText = null;

            TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TMP_Text text = tmpTexts[i];
                if (text == null)
                    continue;

                if (IsScoreValueText(text.gameObject.name) && ShouldPreferScoreText(text, keptTmpScoreText))
                    keptTmpScoreText = text;
            }

            if (keptTmpScoreText != null)
            {
                for (int i = 0; i < tmpTexts.Length; i++)
                {
                    TMP_Text text = tmpTexts[i];
                    if (text == null || !IsScoreValueText(text.gameObject.name))
                        continue;

                    bool isKeptText = text == keptTmpScoreText;
                    text.gameObject.SetActive(isKeptText);
                    if (isKeptText)
                    {
                        text.text = value;
                        text.color = scoreColor;
                        text.alignment = TextAlignmentOptions.Center;
                    }
                }

                HideLegacyScoreTexts();
                return;
            }

            Text keptLegacyScoreText = null;
            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text == null)
                    continue;

                if (IsScoreValueText(text.gameObject.name) && ShouldPreferScoreText(text, keptLegacyScoreText))
                    keptLegacyScoreText = text;
            }

            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text == null || !IsScoreValueText(text.gameObject.name))
                    continue;

                bool isKeptText = text == keptLegacyScoreText;
                text.gameObject.SetActive(isKeptText);
                if (isKeptText)
                {
                    text.text = value;
                    text.color = scoreColor;
                    text.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        private void HideLegacyScoreTexts()
        {
            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text != null && IsScoreValueText(text.gameObject.name))
                    text.gameObject.SetActive(false);
            }
        }

        private static bool ShouldPreferScoreText(TMP_Text candidate, TMP_Text current)
        {
            if (current == null)
                return true;

            return candidate.gameObject.name == "Score Value" && current.gameObject.name != "Score Value";
        }

        private static bool ShouldPreferScoreText(Text candidate, Text current)
        {
            if (current == null)
                return true;

            return candidate.gameObject.name == "Score Value" && current.gameObject.name != "Score Value";
        }

        private static bool IsScoreValueText(string objectName)
        {
            return objectName == "Score Value" || objectName.StartsWith("Score Value (", System.StringComparison.Ordinal);
        }

        private Color ResolvePlayerNameColor()
        {
            TMP_Text[] tmpTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TMP_Text text = tmpTexts[i];
                if (text != null && text.gameObject.name == "Player Name")
                    return text.color;
            }

            Text[] legacyTexts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];
                if (text != null && text.gameObject.name == "Player Name")
                    return text.color;
            }

            return Color.white;
        }

        private void SetChildSprite(string childName, Sprite sprite)
        {
            if (sprite == null)
                return;

            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image != null && image.gameObject.name == childName)
                {
                    image.sprite = sprite;
                    ArrangeAvatarSpriteLayer(image, childName);
                }
            }
        }

        private static void ArrangeAvatarSpriteLayer(Image image, string childName)
        {
            if (image == null)
                return;

            if (childName == "Avatar Frame")
            {
                image.preserveAspect = true;
                Transform parent = image.transform.parent;
                Transform avatarMask = parent != null ? parent.Find("Avatar Mask") : null;
                if (avatarMask != null)
                    image.transform.SetSiblingIndex(avatarMask.GetSiblingIndex());
                return;
            }

            if (childName == "Avatar Portrait")
            {
                image.preserveAspect = false;
                image.transform.SetAsLastSibling();
            }
        }

        private static int ParseRankFromRowName(string rowName)
        {
            if (string.IsNullOrEmpty(rowName) || !rowName.StartsWith("Row "))
                return -1;

            int index = 4;
            int rankValue = 0;
            bool foundDigit = false;
            while (index < rowName.Length && char.IsDigit(rowName[index]))
            {
                rankValue = rankValue * 10 + (rowName[index] - '0');
                foundDigit = true;
                index++;
            }

            return foundDigit ? rankValue : -1;
        }

        private static int ParseRankFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return -1;

            text = text.Trim();
            int rankValue = 0;
            bool foundDigit = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                    return -1;

                rankValue = rankValue * 10 + (text[i] - '0');
                foundDigit = true;
            }

            return foundDigit ? rankValue : -1;
        }
    }
}
