using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using DV.ServicePenalty;
using DVMultiplayer;
using DVMultiplayer.DTO.Debt;
using DVMultiplayer.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class NetworkDebtManager : SingletonBehaviour<NetworkDebtManager>
{
    public bool IsChangeByNetwork { get; internal set; } = false;

    protected override void Awake()
    {
        base.Awake();
        Main.Log($"NetworkDebtManager initialized");
        SingletonBehaviour<UnityClient>.Instance.MessageReceived += MessageReceived;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (SingletonBehaviour<UnityClient>.Instance)
            SingletonBehaviour<UnityClient>.Instance.MessageReceived -= MessageReceived;
    }

    internal void OnJobDeptPaid(string id, bool isDestroyed)
    {
        Main.Log($"[CLIENT] > DEBT_JOB_PAID");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new DebtPaid() { Id = id, isDestroyed = isDestroyed });

            using (Message message = Message.Create((ushort)NetworkTags.DEBT_JOB_PAID, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void MessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage())
        {
            switch ((NetworkTags)message.Tag)
            {
                case NetworkTags.DEBT_LOCO_PAID:
                    OnLocoDeptPaidMessage(message);
                    break;

                case NetworkTags.DEBT_JOB_PAID:
                    OnJobDeptPaidMessage(message);
                    break;

                case NetworkTags.DEBT_OTHER_PAID:
                    OnOtherDeptPaidMessage(message);
                    break;
            }
        }
    }

    private void OnJobDeptPaidMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < DEBT_JOB_PAID");

            while (reader.Position < reader.Length)
            {
                IsChangeByNetwork = true;
                DebtPaid data = reader.ReadSerializable<DebtPaid>();
                if (data.isDestroyed)
                {
                    StagedJobDebt debt = SingletonBehaviour<JobDebtController>.Instance.stagedJobsDebts.FirstOrDefault(t => t.ID == data.Id);
                    if (debt != null)
                        debt.Pay();
                }
                else
                {
                    ExistingJobDebt debt = SingletonBehaviour<JobDebtController>.Instance.existingTrackedJobs.FirstOrDefault(t => t.ID == data.Id);
                    if (debt != null)
                        debt.Pay();
                }
                IsChangeByNetwork = false;
            }
        }
    }

    internal void OnOtherDeptPaid(string id, bool isDestroyed)
    {
        Main.Log($"[CLIENT] > DEBT_OTHER_PAID");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new DebtPaid() { Id = id, isDestroyed = isDestroyed });

            using (Message message = Message.Create((ushort)NetworkTags.DEBT_OTHER_PAID, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }

    private void OnOtherDeptPaidMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < DEBT_OTHER_PAID");

            while (reader.Position < reader.Length)
            {
                IsChangeByNetwork = true;
                DebtPaid data = reader.ReadSerializable<DebtPaid>();
                if (data.isDestroyed)
                {
                    StagedOtherDebt debt = SingletonBehaviour<JobDebtController>.Instance.deletedJoblessCarDebts;
                    if (debt != null && debt.ID == data.Id)
                        debt.Pay();
                }
                else
                {
                    ExistingOtherDebt debt = SingletonBehaviour<JobDebtController>.Instance.existingJoblessCarDebts;
                    if (debt != null && debt.ID == data.Id)
                        debt.Pay();
                }
                IsChangeByNetwork = false;
            }
        }
    }

    private void OnLocoDeptPaidMessage(Message message)
    {
        using (DarkRiftReader reader = message.GetReader())
        {
            Main.Log($"[CLIENT] < DEBT_LOCO_PAID");

            while (reader.Position < reader.Length)
            {
                IsChangeByNetwork = true;
                DebtPaid data = reader.ReadSerializable<DebtPaid>();
                if (data.isDestroyed)
                {

                    StagedLocoDebt debt = SingletonBehaviour<LocoDebtController>.Instance.destroyedLocosDebts.FirstOrDefault(t => t.ID == data.Id);
                    if(debt != null)
                        debt.Pay();
                }
                else
                {
                    ExistingLocoDebt debt = SingletonBehaviour<LocoDebtController>.Instance.trackedLocosDebts.FirstOrDefault(t => t.ID == data.Id);
                    if (debt != null)
                        debt.Pay();
                }
                IsChangeByNetwork = false;
            }
        }
        
    }

    internal void OnLocoDeptPaid(string id, bool isDestroyed)
    {
        Main.Log($"[CLIENT] > DEBT_LOCO_PAID");

        using (DarkRiftWriter writer = DarkRiftWriter.Create())
        {
            writer.Write(new DebtPaid() { Id = id, isDestroyed = isDestroyed });

            using (Message message = Message.Create((ushort)NetworkTags.DEBT_LOCO_PAID, writer))
                SingletonBehaviour<UnityClient>.Instance.SendMessage(message, SendMode.Reliable);
        }
    }
}
