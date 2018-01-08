namespace JustClimbTrial.Interfaces
{
    public interface ISavingVideo
    {
        string TmpVideoFilePath { get; set; }
        bool IsConfirmSaveVideo { get; set; }
    }
}
