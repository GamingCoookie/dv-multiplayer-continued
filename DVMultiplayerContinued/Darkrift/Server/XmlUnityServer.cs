using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Xml.Linq;
using UnityEngine;

namespace DarkRift.Server.Unity
{
    [AddComponentMenu("DarkRift/Server")]
    public class XmlUnityServer : SingletonBehaviour<XmlUnityServer>
    {
        /// <summary>
        ///     The actual server.
        /// </summary>
        public DarkRiftServer Server { get; private set; }

        public ushort port;

#pragma warning disable IDE0044 // Add readonly modifier, Unity can't serialize readonly fields
        [SerializeField]
        [Tooltip("The configuration file to use.")]
        public TextAsset configuration;

        [SerializeField]
        [Tooltip("Indicates whether the server will be created in the OnEnable method.")]
        public bool createOnEnable = false;

        [SerializeField]
        [Tooltip("Indicates whether the server events will be routed through the dispatcher or just invoked.")]
        private bool eventsFromDispatcher = true;
#pragma warning restore IDE0044 // Add readonly modifier, Unity can't serialize readonly fields

        private void OnEnable()
        {
            //If createOnEnable is selected create a server
            if (createOnEnable)
                Create();
        }

        private void Update()
        {
            //Execute all queued dispatcher tasks
            if (Server != null)
                Server.ExecuteDispatcherTasks();
        }

        /// <summary>
        ///     Creates the server.
        /// </summary>
        public void Create()
        {
            Create(new NameValueCollection());
        }

        /// <summary>
        ///     Creates the server.
        /// </summary>
        public void Create(NameValueCollection variables)
        {
            if (Server != null)
                if (Server.Disposed)
                {
                    Server.StartServer();
                }
                else
                    throw new InvalidOperationException("The server has already been created! (Is CreateOnEnable enabled?)");

            if (configuration != null)
            {
                // Create spawn data from config
                ServerSpawnData spawnData = ServerSpawnData.CreateFromXml(XDocument.Parse(configuration.text), variables);
                if (spawnData == null)
                    throw new Exception("SpawnData not defined");

                // Allow only this thread to execute dispatcher tasks to enable deadlock protection
                spawnData.DispatcherExecutorThreadID = Thread.CurrentThread.ManagedThreadId;

                // Inaccessible from XML, set from inspector
                spawnData.EventsFromDispatcher = eventsFromDispatcher;

                // Unity is broken, work around it...
                // This is an obsolete property but is still used if the user is using obsolete <server> tag properties
#pragma warning disable 0618
                spawnData.Server.UseFallbackNetworking = true;
#pragma warning restore 0618

                // Add types
                spawnData.PluginSearch.PluginTypes.AddRange(UnityServerHelper.SearchForPlugins());

                spawnData.Listeners.NetworkListeners[0].Port = port;

                // Create server
                Server = new DarkRiftServer(spawnData);
                Server.StartServer();
            }
            else
                throw new Exception("Configuration file not set");
        }

        private void OnDisable()
        {
            Close();
        }

        private void OnApplicationQuit()
        {
            Close();
        }

        public bool CheckTCPSocketReady()
        {
            bool isAvailable = true;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }
            }

            return !isAvailable;
        }

        /// <summary>
        ///     Closes the server.
        /// </summary>
        public void Close()
        {
            if (Server != null)
                Server.Dispose();
            else
                return;

            Server = null;
        }
    }
}
