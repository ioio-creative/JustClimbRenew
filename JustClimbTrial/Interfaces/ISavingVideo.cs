using JustClimbTrial.Enums;

namespace JustClimbTrial.Interfaces
{
    public interface ISavingVideo
    {
        string RouteId { get; }
        ClimbMode RouteClimbMode { get; }
        bool IsRouteContainDemoVideo { get; }

        string TmpVideoFilePath { get; set; }
        bool IsConfirmSaveVideo { get; set; }

        void DeleteTmpVideoFileSafe();
        void ResetSavingVideoProperties();
    }
}
