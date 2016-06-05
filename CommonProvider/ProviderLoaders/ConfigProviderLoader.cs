﻿using System;
using System.Collections.Generic;
using System.Configuration;
using CommonProvider.Configuration;
using CommonProvider.Data;

namespace CommonProvider.ProviderLoaders
{
    /// <summary>
    /// Represents an implementation of ProviderLoaderBase that retrieves provider 
    /// information from a configuration file. The Config Provider Loader requires 
    /// that information regarding the providers be pre-configured before loading.
    /// </summary>
    public class ConfigProviderLoader : ProviderLoaderBase
    {
        private readonly ProviderConfigSection _configSection;
        private const string SectionName = "commonProvider";

        /// <summary>
        /// Initializes an instance of ConfigProviderLoader
        /// </summary>
        public ConfigProviderLoader()
        {
            _configSection = ConfigurationManager.GetSection(SectionName) as ProviderConfigSection;
        }


        internal ConfigProviderLoader(ProviderConfigSection configSection)
        {
            _configSection = configSection;
        }

        /// <summary>
        /// Loads provider information/meta data from a configuration file.
        /// </summary>
        /// <returns>The loaded providers data.</returns>
        protected override IProviderData PerformLoad()
        {
            if (_configSection == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format("Config section {0} not defined",
                    SectionName));
            }

            var providerDescriptors = new List<IProviderDescriptor>();

            Data.ProviderSettings generalSettings = null;
            Dictionary<string, string> gs = null;

            // set general settings
            foreach (ProviderSettingElement setting in _configSection.Settings)
            {
                if (gs == null)
                    gs = new Dictionary<string, string>();

                gs.Add(setting.Key, setting.Value);
            }

            if (gs != null)
            {
                string dataParserType = string.Empty;
                if (!string.IsNullOrEmpty(_configSection.Settings.DataParserType))
                {
                    dataParserType = GetDataParserType(_configSection,
                        _configSection.Settings.DataParserType);
                }

                generalSettings = new Data.ProviderSettings(gs, dataParserType);
            }

            foreach (ProviderElement providerElement in _configSection.Providers)
            {
                if (!providerElement.IsEnabled) continue;

                // get provider type
                Type providerType = GetProviderType(_configSection, providerElement);
                if (!typeof(IProvider).IsAssignableFrom(providerType))
                {
                    throw new ConfigurationErrorsException(
                        string.Format("Type '{0}' doesn't implement 'IProvider'.", providerType));
                }

                // set custom settings
                Dictionary<string, string> ps = null;
                foreach (ProviderSettingElement setting in providerElement.Settings)
                {
                    if (ps == null)
                        ps = new Dictionary<string, string>();

                    ps.Add(setting.Key, setting.Value);
                }

                // get complex data parser type
                Data.ProviderSettings providerSetting = null;
                if (ps != null)
                {
                    string dataParserType = string.Empty;
                    if (!string.IsNullOrEmpty(providerElement.Settings.DataParserType))
                    {
                        dataParserType = GetDataParserType(_configSection,
                            providerElement.Settings.DataParserType);

                    }
                    providerSetting = new Data.ProviderSettings(ps, dataParserType);
                }

                providerDescriptors.Add(
                    new ProviderDescriptor(
                            providerElement.Name,
                            providerElement.Group,
                            providerType,
                            providerSetting,
                            providerElement.IsEnabled)
                        );
            }

            return new ProviderData(providerDescriptors, generalSettings);
        }

        #region Helpers

        private string GetDataParserType(ProviderConfigSection configSection, string dataParserType)
        {
            string dataParserElementType = GetObjectType(configSection, dataParserType);
            if (!string.IsNullOrEmpty(dataParserElementType))
            {
                if (Type.GetType(dataParserElementType) == null)
                {
                    throw new ConfigurationErrorsException(
                        "The Type defined for the ComplexDataTypeParser is not valid.");
                }
                else
                {
                    return dataParserElementType;
                }
            }
            else
            {
                if (Type.GetType(dataParserType) == null)
                {
                    throw new ConfigurationErrorsException(
                        "No Type found for the ComplexDataTypeParser.");
                }
                else
                {
                    return dataParserType;
                }
            }
        }

        private Type GetProviderType(ProviderConfigSection configSection, ProviderElement providerElement)
        {
            Type providerType = null;
            string providerElementType = GetObjectType(configSection, providerElement.Type);
            if (!string.IsNullOrEmpty(providerElementType))
            {
                providerType = Type.GetType(providerElementType);
                if (providerType == null)
                {
                    throw new ConfigurationErrorsException(
                        string.Format("The Type defined for Provider '{0}' is not valid.",
                        providerElement.Name));
                }
            }
            else
            {
                providerType = Type.GetType(providerElement.Type);
                if (providerType == null)
                {
                    throw new ConfigurationErrorsException(
                        string.Format("No Type found for Provider '{0}'.",
                        providerElement.Name));
                }
            }
            return providerType;
        }

        private string GetObjectType(ProviderConfigSection configSection, string typeName)
        {
            if (configSection.Types == null) return string.Empty;

            string objectType = string.Empty;
            foreach (TypeElement objectTypeElement in configSection.Types)
            {
                if (objectTypeElement.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                {
                    objectType = objectTypeElement.Type;
                    break;
                }
            }
            return objectType;
        }

        #endregion
    }
}