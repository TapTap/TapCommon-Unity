using System;
using System.Collections.Generic;
using System.IO;
using com.taptap.tapsdk.bindings.csharp;
using UnityEngine;
using DeviceType = com.taptap.tapsdk.bindings.csharp.DeviceType;

namespace TapTap.Common.Internal {
    internal static class TapDuration {
        
        private static bool isRndEnvironment;
        private static bool initialized;
        private static UnityTDSUser unityTdsUser;
        private static BridgeUser bridgeUser;
        
        static TapDuration() {
            EventManager.AddListener(EventConst.SetRND, (_) => isRndEnvironment = true);
        }
        
        internal static void Init(TapConfig config) {
            if (!TapCommon.DisableDurationStatistics && !isRndEnvironment && !initialized) {
                DurationInit();
                initialized = true;
            }   
        }
        
        private static void DurationInit() {
            try {
                if (SupportDurationStatistics())
                    DurationBindingInit();
            }
            catch (Exception e) {
                while (e.InnerException != null) {
                    e = e.InnerException;
                }
                TapLogger.Error("[TapSDK::Duration] Init Error Won't statistic duration info! Error info: " + e.ToString() + "\n" + e.StackTrace);
            }
        }
        
        private static bool SupportDurationStatistics() {
    #if UNITY_EDITOR
            return false;
    #elif UNITY_STANDALONE_OSX
            return false;
    #else
            return true;
    #endif
        }

        private static void DurationBindingInit() {
            BindGameConfig();
            BindUserInfo();
            BindWindowChange();
        }

        private static void BindGameConfig() {
            var bridgeConfig = new BridgeConfig();
            var dir = new DirectoryInfo(Path.Combine(Application.persistentDataPath, "tapsdk"));
            if (!dir.Exists)
                dir.Create();
            bridgeConfig.cache_dir = dir.FullName;
            bridgeConfig.ca_dir = "";
            bridgeConfig.device_id = SystemInfo.deviceUniqueIdentifier;
            bridgeConfig.enable_duration_statistics = true;
            bridgeConfig.device_type = (int)DeviceType.Local;
            Bindings.InitSDK(bridgeConfig);
            // Set Game
            var bridgeGame = new BridgeGame();
            bridgeGame.client_id = TapCommon.Config.ClientID;
            bridgeGame.identify = Application.identifier;
            Bindings.SetCurrentGame(bridgeGame);
        }
        
        private static void BindUserInfo() {
            unityTdsUser = new UnityTDSUser();
            
            EventManager.AddListener(EventConst.OnTapLogin, (loginParameter) => {
                var kv = loginParameter is KeyValuePair<string, string> ? (KeyValuePair<string, string>)loginParameter : default;
                if (!string.IsNullOrEmpty(kv.Key)) {
                    if (unityTdsUser.IsEmpty) {
                        bridgeUser = new BridgeUser();
                        Bindings.SetCurrentUser(bridgeUser);
                    }
                    unityTdsUser.UpdateUserInfo(kv.Key, kv.Value);
                    bridgeUser.user_id = unityTdsUser.GetUserId();
                    bridgeUser.contain_tap_info = unityTdsUser.ContainTapInfo();
                }
            });
            EventManager.AddListener(EventConst.OnTapLogout, (logoutChannel) => {
                if (logoutChannel is string channel && !string.IsNullOrEmpty(channel)) {
                    unityTdsUser.Logout();
                    Bindings.SetCurrentUser(null);
                }
            });
            
            EventManager.AddListener(EventConst.OnBind, (kv) => {
                if (!(kv is KeyValuePair<string, string>)) return;
                if (unityTdsUser.IsEmpty) return;
                var bindInfo = (KeyValuePair<string, string>)kv;
                if (!string.IsNullOrEmpty(bindInfo.Key)) {
                    unityTdsUser.UpdateUserInfo(bindInfo.Key, bindInfo.Value);
                    bridgeUser.user_id = unityTdsUser.GetUserId();
                    bridgeUser.contain_tap_info = unityTdsUser.ContainTapInfo();
                }
            });
        }
        
        private static void BindWindowChange() {
            EventManager.AddListener(EventConst.OnApplicationPause, (isPause) => {
                var isPauseBool = (bool)isPause;
                if (isPauseBool) {
                    Bindings.OnWindowBackground();
                }
                else {
                    Bindings.OnWindowForeground();
                }
            });
        }
    }
}