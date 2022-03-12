using DVMultiplayer;
using UnityEngine;

internal class NetworkJunctionSync : MonoBehaviour
{
    private Junction junction;

    public uint Id { get; internal set; }
#pragma warning disable IDE0051 // Remove unused private members
    private void Awake()
    {
        Main.Log($"NetworkJunctionSync initalized");
        junction = GetComponent<Junction>();
        Main.Log($"NetworkJunctionSync Listening to Junction change event");
        junction.Switched += OnJunctionSwitched;
    }

    private void OnDestroy()
    {
        junction.Switched -= OnJunctionSwitched;
    }
#pragma warning restore IDE0051

    private void OnJunctionSwitched(Junction.SwitchMode mode, int branchNum)
    {
        if (SingletonBehaviour<NetworkJunctionManager>.Instance.IsChangeByNetwork)
            return;

        SingletonBehaviour<NetworkJunctionManager>.Instance.OnJunctionSwitched(Id, mode, branchNum == 0);
    }
}