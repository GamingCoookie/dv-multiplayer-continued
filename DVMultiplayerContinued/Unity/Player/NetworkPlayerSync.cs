using UnityEngine;
using UnityEngine.UI;

internal class NetworkPlayerSync : MonoBehaviour
{
    public TrainCar Train { get; set; }
    public bool IsLocal { get; set; } = false;
    public string Username { get; set; }
    public string[] Mods { get; set; }
    public ushort Color { get; set; }
    internal ushort Id;
    private Vector3 prevPosition;
    private Vector3 newPosition;
    internal bool IsLoaded;
    private int ping;
    private long updatedAt;
    private Text pingText;

#pragma warning disable IDE0051 // Remove unused private members
    private void Start()
    {
        newPosition = transform.position - WorldMover.currentMove;
        prevPosition = newPosition;
    }

    private void Update()
    {
        Vector3 position = transform.position;
        if (!IsLocal)
        {
            if (Vector3.Distance(position, newPosition + WorldMover.currentMove) >= 2)
            {
                transform.position = newPosition + WorldMover.currentMove;
            }
            else if (position != newPosition + WorldMover.currentMove)
            {
                float increment = 15;
                if (Train)
                {
                    increment = Train.GetVelocity().magnitude * 3.6f;
                    if (increment <= 0.1f)
                        increment = 1;
                }

                float step = increment * Time.deltaTime;
                transform.position = Vector3.MoveTowards(position, newPosition + WorldMover.currentMove, step);
            }

            if (!pingText) pingText = transform.GetChild(0).Find("Ping").GetComponent<Text>();
            pingText.text = $"{ping}ms";
            return;
        }

        if (Vector3.Distance(prevPosition, position) > 1e-5)
        {
            SingletonBehaviour<NetworkPlayerManager>.Instance.UpdateLocalPositionAndRotation(position, transform.rotation);
            prevPosition = position;
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
