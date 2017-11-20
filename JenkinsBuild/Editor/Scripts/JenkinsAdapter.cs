﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using Jenkins.XmlConst;
using UnityEditor;
using UnityEngine;

namespace Jenkins
{
    public class JenkinsAdapter
    {
        public static List<string> Scenes = new List<string>();
        public static Dictionary<string,string> Config = new Dictionary<string, string>();

//        [MenuItem("Jenkins/Test Xml")]
//        public static void _TestXml()
//        {
//            _getXmlVale(
//                @"I:\GitHubProject\JenkinsBuildTest\Assets\Plugins\JenkinsBuild\JenkinsBuild\Editor\Config\AndroidBuildInfo.config");
//        }

        /// <summary>
        /// 解析命令行传过来的xml
        /// </summary>
        public static void XmlBuild()
        {

            Debug.Log("命令行参数个数:"+ Environment.GetCommandLineArgs().Length);
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                Debug.Log("参数:"+arg);
            }
            var count = Environment.GetCommandLineArgs().Length;
            _getXmlVale(Environment.GetCommandLineArgs()[count - 1]);

        }

        /// <summary>
        /// 获取xml的值
        /// </summary>
        /// <param name="path">xml所在路径</param>
        private static void _getXmlVale(string path)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(path);
            var root = xml.DocumentElement;
            _getScene(root);
            _getOther(root);
            foreach (var pair in Config)
            {
                Debug.Log("xml配置:key=" + pair.Key + ", Value=" + pair.Value);
            }
        }

        /// <summary>
        /// 获取除了场景外的所有配置
        /// </summary>
        /// <param name="xml"></param>
        private static void _getOther(XmlElement xml)
        {
            Config.Clear();
            foreach (XmlElement node in xml)
            {
                //因为场景是单独处理所以跳过
                if (node.Name == ConfigNodeConst.Scences)
                {
                    continue;
                }

                if (node.Attributes.GetNamedItem(XmlAttributeConst.Min) != null)
                {
                    _getSdkVersions(node);
                    continue;
                }
                string value = node.InnerText;
                if (string.IsNullOrEmpty(node.InnerText))
                {
                    value = node.GetAttribute(XmlAttributeConst.Default);
                }
                Config.Add(node.Name, value);
            }
        }

        /// <summary>
        /// 特殊处理的sdk版本
        /// </summary>
        /// <param name="node"></param>
        private static void _getSdkVersions(XmlElement node)
        {
            int minVersions = int.Parse(node.GetAttribute(XmlAttributeConst.Min));
            int maxVersions = int.Parse(node.GetAttribute(XmlAttributeConst.Max));
            int defaultVersions = int.Parse(node.GetAttribute(XmlAttributeConst.Default));

            int currenVersions = 0;

            if (string.IsNullOrEmpty(node.InnerText))
            {
                currenVersions = int.Parse(node.GetAttribute(XmlAttributeConst.Default));
            }
            //如果不是等于默认值的话就进行判断
            if (currenVersions != defaultVersions)
            {
                //如果大于最大的版本设置为最大
                if (currenVersions > maxVersions)
                {
                    currenVersions = maxVersions;
                }

                //如果小于最小版本设置成最小
                if (currenVersions < minVersions)
                {
                    currenVersions = minVersions;
                }
            }
            Config.Add(node.Name,currenVersions.ToString());
        }

        /// <summary>
        /// 特殊处理的场景信息获取
        /// </summary>
        /// <param name="xml"></param>
        private static void _getScene(XmlElement xml)
        {
            var scences = xml.GetElementsByTagName(ConfigNodeConst.Scences);
            foreach (XmlNode childNode in scences[0].ChildNodes)
            {
                //explain="入口场景"
                var attributes = childNode.Attributes;
                Debug.Log("场景:" + childNode.InnerText + "描述:" +
                    (
                    attributes != null
                        ?
                        attributes.GetNamedItem(XmlAttributeConst.Explain) != null
                            ?
                        attributes.GetNamedItem(XmlAttributeConst.Explain).InnerText : "没有描述"
                   : "没有描述"));
                Scenes.Add(childNode.InnerText);
            }
        }

        [MenuItem("Jenkins/JenkinsBuildAndroid")]
        public static void CommandLineXmlBuildAndroid()
        {
            //解析XML
            XmlBuild();
            _setBuildAndroidInfo();
            //todo 输出路径需要需改为读取配置而不是写死
            var path = BuildPipeline.BuildPlayer(Scenes.ToArray(), Config[ConfigNodeConst.Path], BuildTarget.Android, BuildOptions.None);
            Debug.Log("Build Complete Path:" + path);
        }

        /// <summary>
        /// 安卓打包设置
        /// </summary>
        private static void _setBuildAndroidInfo()
        {
            PlayerSettings.Android.bundleVersionCode = int.Parse(Config[ConfigNodeConst.BundleVersionCode]);
            PlayerSettings.Android.minSdkVersion = _stringToEnum<AndroidSdkVersions>(Config[AndroidAndIosConfigNodeConfig.SdkVersions]);
            PlayerSettings.Android.targetSdkVersion = _stringToEnum<AndroidSdkVersions>(Config[AndroidAndIosConfigNodeConfig.TargetSdkVersion]);
            PlayerSettings.Android.targetDevice = _stringToEnum<AndroidTargetDevice>(Config[AndroidAndIosConfigNodeConfig.TargetDevice]);
            PlayerSettings.Android.forceInternetPermission = bool.Parse(Config[AndroidConfigNodeConst.InternetAccess]);
            _setBuildInfo(BuildTargetGroup.Android);
        }

//        private static void _setBuildIosInfo(BuildSettingInfo buildInfo)
//        {
//            PlayerSettings.iOS.buildNumber = buildInfo.BundleVersionCode;
//            PlayerSettings.iOS.targetDevice = _stringToEnum<iOSTargetDevice>(buildInfo.TargetDevice);
//            PlayerSettings.iOS.targetOSVersionString = buildInfo.TargetSdkVersion;
////            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK
//           //            PlayerSettings.Android.minSdkVersion = _stringToEnum<AndroidSdkVersions>(buildInfo.AndroidSdkVersions);
//           //            PlayerSettings.Android.targetSdkVersion = _stringToEnum<AndroidSdkVersions>(buildInfo.TargetSdkVersion);
//           //            PlayerSettings.Android.targetDevice = _stringToEnum<AndroidTargetDevice>(buildInfo.TargetDevice);
//            PlayerSettings.Android.forceInternetPermission = buildInfo.InternetAccess;
//            _setBuildInfo(BuildTargetGroup.Android, buildInfo);
//        }

        /// <summary>
        /// 平台共有设置
        /// </summary>
        /// <param name="target"></param>
        private static void _setBuildInfo(BuildTargetGroup target)
        {
            PlayerSettings.bundleVersion = Config[ConfigNodeConst.Version];
            PlayerSettings.applicationIdentifier = Config[ConfigNodeConst.PackName];
            PlayerSettings.SetScriptingBackend(target, _stringToEnum<ScriptingImplementation>(Config[ConfigNodeConst.Scriptingimplementation]));
            PlayerSettings.SetApiCompatibilityLevel(target, _stringToEnum<ApiCompatibilityLevel>(Config[ConfigNodeConst.ApiCompatibilityLevel]));
            
            EditorUserBuildSettings.development = bool.Parse(Config[ConfigNodeConst.Development]);
            EditorUserBuildSettings.connectProfiler = bool.Parse(Config[ConfigNodeConst.ConnectProfiler]);
            EditorUserBuildSettings.allowDebugging = bool.Parse(Config[ConfigNodeConst.ScriptsDebuggers]);

        }

       
        static T _stringToEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

//        [MenuItem("Jenkins/JenkinsBuildIos")]
//        public static void CommandLineBuildIos()
//        {
////            BuildPipeline.BuildPlayer(_analysisLineArgs(), GetIosBuildPath(), BuildTarget.iOS, BuildOptions.None);
//            Debug.Log("Build Complete Path:" + GetIosBuildPath());
//        }

//        [MenuItem("Jenkins/JenkinsBuildWindows")]
//        public static void CommandLineBuildWin()
//        {
////            BuildPipeline.BuildPlayer(_analysisLineArgs(), GetWindowsPath(), BuildTarget.StandaloneWindows, BuildOptions.None);
//            Debug.Log("Build Complete Path:" + GetWindowsPath());
//        }

    }

}


