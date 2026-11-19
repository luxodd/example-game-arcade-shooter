using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Luxodd.Game.Scripts.Input;

namespace Luxodd.Game.Scripts.Runtime.UI
{
    public sealed class ArcadeControlLegendEntryView : MonoBehaviour
    {
        [SerializeField] private Image _buttonMarker;
        [SerializeField] private TMP_Text _buttonNumberLabel;
        [SerializeField] private TMP_Text _actionLabel;
        [SerializeField] private ArcadeButtonColor _buttonColor;

        internal void Apply(ArcadeButtonColor buttonColor, string actionLabel)
        {
            _buttonColor = buttonColor;

            if (_buttonMarker != null)
                _buttonMarker.color = GetButtonColor(buttonColor);

            if (_buttonNumberLabel != null)
            {
                _buttonNumberLabel.text = GetButtonNumber(buttonColor);
                _buttonNumberLabel.color = GetButtonNumberColor(buttonColor);
            }

            if (_actionLabel != null)
                _actionLabel.text = actionLabel?.Trim() ?? string.Empty;

            gameObject.SetActive(!string.IsNullOrWhiteSpace(actionLabel));
        }

        private void Reset()
        {
            _buttonMarker = GetComponentInChildren<Image>(true);

            if (_buttonMarker != null)
                _buttonNumberLabel = _buttonMarker.GetComponentInChildren<TMP_Text>(true);

            TMP_Text[] textLabels = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < textLabels.Length; i++)
            {
                if (textLabels[i] != _buttonNumberLabel)
                {
                    _actionLabel = textLabels[i];
                    break;
                }
            }
        }

        private static string GetButtonNumber(ArcadeButtonColor buttonColor)
        {
            switch (buttonColor)
            {
                case ArcadeButtonColor.Black:
                    return "1";
                case ArcadeButtonColor.Red:
                    return "2";
                case ArcadeButtonColor.Green:
                    return "3";
                case ArcadeButtonColor.Yellow:
                    return "4";
                case ArcadeButtonColor.Blue:
                    return "5";
                case ArcadeButtonColor.Purple:
                    return "6";
                case ArcadeButtonColor.Orange:
                    return "7";
                case ArcadeButtonColor.White:
                    return "8";
                default:
                    return string.Empty;
            }
        }

        private static Color GetButtonNumberColor(ArcadeButtonColor buttonColor)
        {
            switch (buttonColor)
            {
                //case ArcadeButtonColor.Green:
                case ArcadeButtonColor.Yellow:
                case ArcadeButtonColor.White:
                    return Color.black;
                default:
                    return Color.white;
            }
        }

        private static Color GetButtonColor(ArcadeButtonColor buttonColor)
        {
            switch (buttonColor)
            {
                case ArcadeButtonColor.Black:
                    return Color.black;
                case ArcadeButtonColor.Red:
                    return new Color(1f, 0f, 0.0067567825f, 1f);
                case ArcadeButtonColor.Green:
                    return new Color(0.1808063f, 0.6981132f, 0f, 1f);
                case ArcadeButtonColor.Yellow:
                    return new Color(1f, 0.7874013f, 0.033018887f, 1f);
                case ArcadeButtonColor.Blue:
                    return new Color(0.011764705f, 0.32532018f, 0.94509804f, 1f);
                case ArcadeButtonColor.Purple:
                    return new Color(0.5995219f, 0f, 0.9056604f, 1f);
                case ArcadeButtonColor.Orange:
                    return new Color(1f, 0.5f, 0f, 1f);
                case ArcadeButtonColor.White:
                    return Color.white;
                default:
                    return Color.white;
            }
        }
    }
}
