﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JustClimbTrial.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.7.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles")]
        public string AppFilesDirectory {
            get {
                return ((string)(this["AppFilesDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".mp4")]
        public string VideoFileExtension {
            get {
                return ((string)(this["VideoFileExtension"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=DESKTOP-NSE6A6A\\SQLEXPRESS;Initial Catalog=JustClimb;User ID=IoioSa;P" +
            "assword=Ioio0512")]
        public string JustClimbConnectionString {
            get {
                return ((string)(this["JustClimbConnectionString"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=ioio-aws.cpj7x1dsbh3k.ap-southeast-1.rds.amazonaws.com;Initial Catalo" +
            "g=JustClimb;User ID=IoioSa;Password=Ioio0512;Persist Security Info=True;")]
        public string JustClimbConnectionString1 {
            get {
                return ((string)(this["JustClimbConnectionString1"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\temp\\VideoImgBuffer")]
        public string VideoBufferDirectory {
            get {
                return ((string)(this["VideoBufferDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\WallLog")]
        public string WallLogDirectory {
            get {
                return ((string)(this["WallLogDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\Videos")]
        public string VideoResourcesDirectory {
            get {
                return ((string)(this["VideoResourcesDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\ImgSequences\\BoulderButton\\Lite\\1")]
        public string BoulderButtonNormalImgSequenceDirectory {
            get {
                return ((string)(this["BoulderButtonNormalImgSequenceDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\temp\\TempRecording")]
        public string VideoTempDirectory {
            get {
                return ((string)(this["VideoTempDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\RecordedVideos\\Wall_{0}\\BoulderRoutes\\Route_{1}\\{2}{3}")]
        public string BoulderRouteVideoRecordedFilePathFormat {
            get {
                return ((string)(this["BoulderRouteVideoRecordedFilePathFormat"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("ffmpeg.exe")]
        public string FfmpegExePath {
            get {
                return ((string)(this["FfmpegExePath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\RecordedVideos\\Wall_{0}\\TrainingRoutes\\Route_{1}\\{2}{3}")]
        public string TrainingRouteVideoRecordedFilePathFormat {
            get {
                return ((string)(this["TrainingRouteVideoRecordedFilePathFormat"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("15")]
        public int MaxVideoRecordDurationInMinutes {
            get {
                return ((int)(this["MaxVideoRecordDurationInMinutes"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsDebugMode {
            get {
                return ((bool)(this["IsDebugMode"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\Videos\\Start.mp4")]
        public string GameplayStartVideoPath {
            get {
                return ((string)(this["GameplayStartVideoPath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\Videos\\Countdown.mp4")]
        public string GameplayCountdownVideoPath {
            get {
                return ((string)(this["GameplayCountdownVideoPath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\Videos\\Finish.mp4")]
        public string GameplayFinishVideoPath {
            get {
                return ((string)(this["GameplayFinishVideoPath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\Videos\\Ready.mp4")]
        public string GameplayReadyVideoPath {
            get {
                return ((string)(this["GameplayReadyVideoPath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\ImgSequences\\BoulderButton\\Lite\\1")]
        public string BoulderButtonShowImgSequenceDirectory {
            get {
                return ((string)(this["BoulderButtonShowImgSequenceDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\ImgSequences\\BoulderButton\\Lite\\4")]
        public string BoulderButtonFeedbackImgSequenceDirectory {
            get {
                return ((string)(this["BoulderButtonFeedbackImgSequenceDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\ImgSequences\\BoulderButton\\Lite\\7b")]
        public string BoulderButtonShinePopImgSequenceDirectory {
            get {
                return ((string)(this["BoulderButtonShinePopImgSequenceDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\ImgSequences\\BoulderButton\\Lite\\7c")]
        public string BoulderButtonShineLoopImgSequenceDirectory {
            get {
                return ((string)(this["BoulderButtonShineLoopImgSequenceDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\ImgSequences\\BoulderButton\\Lite\\a")]
        public string BoulderButtonShineFeedbackPopImgSequenceDirectory {
            get {
                return ((string)(this["BoulderButtonShineFeedbackPopImgSequenceDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\ImgSequences\\BoulderButton\\Lite\\b")]
        public string BoulderButtonShineFeedbackLoopImgSequenceDirectory {
            get {
                return ((string)(this["BoulderButtonShineFeedbackLoopImgSequenceDirectory"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1,2,3,4")]
        public string WallPlaneStr {
            get {
                return ((string)(this["WallPlaneStr"]));
            }
            set {
                this["WallPlaneStr"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("..\\AppFiles\\Resources\\Videos\\Gameover.mp4")]
        public string GameOverVideoPath {
            get {
                return ((string)(this["GameOverVideoPath"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsFullScreen {
            get {
                return ((bool)(this["IsFullScreen"]));
            }
        }
    }
}
