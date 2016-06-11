using UnityEngine;
using System.Collections;

public class NullResetter : Resetter
{


    public override void InitializeReset() { }

    public override void ApplyResetting() { }

    public override void FinalizeReset() { }

    public override void SimulatedWalkerUpdate() { }

    public override bool IsResetRequired() { Debug.LogWarning("Null Reset Fail"); return false; }
}
