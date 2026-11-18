using UnityEngine;
using UnityEngine.UI;
using Luxodd.Game.Scripts.Input;

namespace Luxodd.Game.Scripts.Runtime.UI
{
    public sealed class ArcadeControlLegendView : MonoBehaviour
    {
        [SerializeField] private ArcadeControlLegendConfig _config;
        [SerializeField] private ArcadeControlLegendEntryView _blackEntry;
        [SerializeField] private ArcadeControlLegendEntryView _redEntry;
        [SerializeField] private ArcadeControlLegendEntryView _greenEntry;
        [SerializeField] private ArcadeControlLegendEntryView _yellowEntry;
        [SerializeField] private ArcadeControlLegendEntryView _blueEntry;
        [SerializeField] private ArcadeControlLegendEntryView _purpleEntry;

        private ArcadeControlLegendEntryView _orangeEntry;
        private ArcadeControlLegendEntryView _whiteEntry;

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Start()
        {
            FitEntriesToAvailableWidth();
        }

        public void Refresh()
        {
            EnsureReservedSystemEntries();
            FitEntriesToAvailableWidth();

            ApplyEntry(_blackEntry, ArcadeButtonColor.Black, _config != null ? _config.BlackAction : null);
            ApplyEntry(_redEntry, ArcadeButtonColor.Red, _config != null ? _config.RedAction : null);
            ApplyEntry(_greenEntry, ArcadeButtonColor.Green, _config != null ? _config.GreenAction : null);
            ApplyEntry(_yellowEntry, ArcadeButtonColor.Yellow, _config != null ? _config.YellowAction : null);
            ApplyEntry(_blueEntry, ArcadeButtonColor.Blue, _config != null ? _config.BlueAction : null);
            ApplyEntry(_purpleEntry, ArcadeButtonColor.Purple, _config != null ? _config.PurpleAction : null);
            ApplyEntry(_orangeEntry, ArcadeButtonColor.Orange, "Help");
            ApplyEntry(_whiteEntry, ArcadeButtonColor.White, "Menu");
        }

        private void EnsureReservedSystemEntries()
        {
            if (_purpleEntry == null)
                return;

            Transform parent = _purpleEntry.transform.parent;
            if (parent == null)
                return;

            _orangeEntry = FindOrCreateEntry(parent, "OrangeEntry");
            _whiteEntry = FindOrCreateEntry(parent, "WhiteEntry");

            if (_orangeEntry != null)
                _orangeEntry.transform.SetSiblingIndex(_purpleEntry.transform.GetSiblingIndex() + 1);

            if (_whiteEntry != null)
                _whiteEntry.transform.SetSiblingIndex(_orangeEntry != null
                    ? _orangeEntry.transform.GetSiblingIndex() + 1
                    : _purpleEntry.transform.GetSiblingIndex() + 1);
        }

        private ArcadeControlLegendEntryView FindOrCreateEntry(Transform parent, string entryName)
        {
            Transform existingTransform = parent.Find(entryName);
            if (existingTransform != null)
                return existingTransform.GetComponent<ArcadeControlLegendEntryView>();

            ArcadeControlLegendEntryView entry = Instantiate(_purpleEntry, parent, false);
            entry.gameObject.name = entryName;
            return entry;
        }

        private void FitEntriesToAvailableWidth()
        {
            if (_purpleEntry == null || _orangeEntry == null || _whiteEntry == null)
                return;

            Transform parent = _purpleEntry.transform.parent;
            if (parent == null)
                return;

            HorizontalLayoutGroup layoutGroup = parent.GetComponent<HorizontalLayoutGroup>();
            RectTransform parentRect = parent as RectTransform;
            if (layoutGroup == null || parentRect == null || parentRect.rect.width <= 0f)
                return;

            float availableWidth = parentRect.rect.width - layoutGroup.padding.horizontal -
                                   layoutGroup.spacing * 7;
            float entryWidth = availableWidth / 8f;
            if (entryWidth <= 0f)
                return;

            SetEntryWidth(_blackEntry, entryWidth);
            SetEntryWidth(_redEntry, entryWidth);
            SetEntryWidth(_greenEntry, entryWidth);
            SetEntryWidth(_yellowEntry, entryWidth);
            SetEntryWidth(_blueEntry, entryWidth);
            SetEntryWidth(_purpleEntry, entryWidth);
            SetEntryWidth(_orangeEntry, entryWidth);
            SetEntryWidth(_whiteEntry, entryWidth);
        }

        private static void SetEntryWidth(ArcadeControlLegendEntryView entry, float width)
        {
            if (entry == null)
                return;

            RectTransform entryRect = entry.transform as RectTransform;
            if (entryRect != null && entryRect.rect.width > width)
                entryRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private static void ApplyEntry(
            ArcadeControlLegendEntryView entry,
            ArcadeButtonColor buttonColor,
            string actionLabel)
        {
            if (entry != null)
                entry.Apply(buttonColor, actionLabel);
        }
    }
}
