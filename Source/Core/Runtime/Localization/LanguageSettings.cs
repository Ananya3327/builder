// Copyright (c) 2013-2019 Innoactive GmbH
// Licensed under the Apache License, Version 2.0
// Modifications copyright (c) 2021-2022 MindPort GmbH

using UnityEngine.Localization.Settings;
using VRBuilder.Core.Runtime.Utils;

namespace VRBuilder.Core.Internationalization
{
    /// <summary>
    /// Language settings for VR Builder.
    /// </summary>
    public class LanguageSettings : SettingsObject<LanguageSettings>
    {
        /// <summary>
        /// Language which should be used.
        /// </summary>
        public string ApplicationLanguage = "En";

        /// <summary>
        /// Returns the currently active language, will be stored for one session.
        /// </summary>
        public string ActiveLanguage { get; set; }

        /// <summary>
        /// Returns the active or default language.
        /// </summary>
        public string ActiveOrDefaultLanguage
        {
            get
            {
                if (LocalizationSettings.Instance != null && LocalizationSettings.SelectedLocale!=null)
                {
                    return LocalizationSettings.SelectedLocale.Identifier.Code.ToUpper();
                }

                if (string.IsNullOrEmpty(ActiveLanguage))
                {
                    return ApplicationLanguage;
                }

                return ActiveLanguage;
            }
        }
    }
}
