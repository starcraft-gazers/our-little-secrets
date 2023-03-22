namespace Content.FireStationServer._Craft.StationGoals.Graph;

internal enum ExecuteState
{
    Idle,
    InProgress,
    WaitingDelay,
    Finished,
    Interrupted,
    InnerInterrupted
}
