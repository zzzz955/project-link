using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class LanguageSelector : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown dropdown;

        bool _isRefreshing;

        void Awake()
        {
            if (dropdown == null)
                dropdown = GetComponent<TMP_Dropdown>();
        }

        void OnEnable()
        {
            if (dropdown == null)
                return;

            dropdown.onValueChanged.AddListener(OnValueChanged);
            LocalizationManager.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnValueChanged);

            LocalizationManager.LanguageChanged -= Refresh;
        }

        public void Refresh()
        {
            if (dropdown == null || LocalizationManager.Instance == null)
                return;

            _isRefreshing = true;
            dropdown.ClearOptions();
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_us")));
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_kr")));
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_cn")));
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_tw")));
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_th")));
            dropdown.value = ToIndex(LocalizationManager.Instance.CurrentLanguage);
            dropdown.RefreshShownValue();
            _isRefreshing = false;
        }

        void OnValueChanged(int index)
        {
            if (_isRefreshing)
                return;

            LocalizationManager.SetLanguage(ToLanguage(index));
        }

        static int ToIndex(LanguageCode language)
        {
            return language switch
            {
                LanguageCode.KO => 1,
                LanguageCode.ZH_CN => 2,
                LanguageCode.ZH_TW => 3,
                LanguageCode.TH => 4,
                _ => 0,
            };
        }

        static LanguageCode ToLanguage(int index)
        {
            return index switch
            {
                1 => LanguageCode.KO,
                2 => LanguageCode.ZH_CN,
                3 => LanguageCode.ZH_TW,
                4 => LanguageCode.TH,
                _ => LanguageCode.EN,
            };
        }
    }
}
