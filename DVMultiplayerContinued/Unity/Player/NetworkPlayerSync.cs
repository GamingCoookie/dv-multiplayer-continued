using UnityEngine;
using UnityEngine.UI;

internal class NetworkPlayerSync : MonoBehaviour
{
    public TrainCar Train { get; set; }
    public bool IsLocal { get; set; } = false;
    public string Username { get; set; }
    public string[] Mods { get; set; }
    public string Color { get; set; }
    internal ushort Id;
    private Vector3 prevPosition;
    private Vector3 newPosition;
    internal bool IsLoaded;
    private int ping = 0;
    private long updatedAt;

#pragma warning disable IDE0051 // Remove unused private members
    private void Start()
    {
        newPosition = transform.position - WorldMover.currentMove;
    }

    private void Update()
    {
        if (!IsLocal)
        {
            if(Vector3.Distance(transform.position, newPosition + WorldMover.currentMove) >= 2)
            {
                transform.position = newPosition + WorldMover.currentMove;
            }
            else if (transform.position != newPosition + WorldMover.currentMove)
            {
                float increment = 15;
                if (Train)
                {
                    increment = Train.GetVelocity().magnitude * 3.6f;
                    if (increment <= .1f)
                        increment = 1;
                }
                float step = increment * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, newPosition + WorldMover.currentMove, step);
            }
            //transform.position = newPosition + WorldMover.currentMove;
            transform.GetChild(0).Find("Ping").GetComponent<Text>().text = $"{ping}ms";
            return;
        }

        if (prevPosition == null || Vector3.Distance(prevPosition, transform.position) > 1e-5)
        {
            // Main.DebugLog("Player location changed sending new location");
            SingletonBehaviour<NetworkPlayerManager>.Instance.UpdateLocalPositionAndRotation(transform.position, transform.rotation);
            prevPosition = transform.position;
        }
    }
#pragma warning restore IDE0051 // Remove unused private members

    public void UpdateLocation(Vector3 pos, int ping, long updatedAt, Quaternion? rot = null)
    {
        if(updatedAt > this.updatedAt)
        {
            this.updatedAt = updatedAt;
            newPosition = pos;
            this.ping = ping;
            if (rot.HasValue)
                transform.rotation = rot.Value;
        }
    }
}
