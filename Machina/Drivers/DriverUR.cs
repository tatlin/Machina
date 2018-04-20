﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Net.Sockets;

using System.Threading;

using Machina.Drivers.Communication;

namespace Machina.Drivers
{

    //  ██╗   ██╗██████╗ 
    //  ██║   ██║██╔══██╗
    //  ██║   ██║██████╔╝
    //  ██║   ██║██╔══██╗
    //  ╚██████╔╝██║  ██║
    //   ╚═════╝ ╚═╝  ╚═╝
    //                   
    class DriverUR : Driver
    {
        private TCPCommunicationManagerUR _tcpManager;

        private Dictionary<ConnectionType, bool> _availableConnectionTypes = new Dictionary<ConnectionType, bool>()
        {
            { ConnectionType.User, true },
            { ConnectionType.Machina, false }
        };
        public override Dictionary<ConnectionType, bool> AvailableConnectionTypes { get { return _availableConnectionTypes; } }


        //  ██████╗ ██╗   ██╗██████╗ ██╗     ██╗ ██████╗
        //  ██╔══██╗██║   ██║██╔══██╗██║     ██║██╔════╝
        //  ██████╔╝██║   ██║██████╔╝██║     ██║██║     
        //  ██╔═══╝ ██║   ██║██╔══██╗██║     ██║██║     
        //  ██║     ╚██████╔╝██████╔╝███████╗██║╚██████╗
        //  ╚═╝      ╚═════╝ ╚═════╝ ╚══════╝╚═╝ ╚═════╝
        //                                                                             
        /// <summary>
        /// Main constructor
        /// </summary>
        public DriverUR(Control ctrl) : base(ctrl) { }

        /// <summary>
        /// Start a TCP connection to device via its address on the network.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public override bool ConnectToDevice(string ip, int port)
        {
            _tcpManager = new TCPCommunicationManagerUR(this, this.WriteCursor, this.parentControl.motionCursor, ip, port);  // @TODO: the motionCursor should be part of the driver props?

            if (_tcpManager.Connect())
            {
                this.IP = ip;
                this.Port = port;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs all necessary actions to establish a connection to a real/virtual device, 
        /// including connecting to the controller, loggin in, checking required states, etc.
        /// </summary>
        /// <param name="deviceId"></param>
        public override bool ConnectToDevice(int deviceId)
        {
            throw new Exception("Can only connect to ConnectToDevice(int deviceId) in ConnectionType.Machina mode");
            
            return false;
        }

        /// <summary>
        /// Forces disconnection from current controller and manages associated logoffs, disposals, etc.
        /// </summary>
        /// <returns></returns>
        public override bool DisconnectFromDevice()
        {
            bool success = true;

            if (_tcpManager != null)
            {
                success &= _tcpManager.Disconnect();
            }

            return success;
        }

        public override bool Dispose()
        {
            return DisconnectFromDevice();
        }

        public override Joints GetCurrentJoints()
        {
            throw new NotImplementedException();
        }

        public override Rotation GetCurrentOrientation()
        {
            throw new NotImplementedException();
        }

        public override Vector GetCurrentPosition()
        {
            throw new NotImplementedException();
        }

        public override void DebugDump()
        {
            throw new NotImplementedException();
        }

        public override void Reset()
        {
            throw new NotImplementedException();
        }





        //███████╗████████╗██████╗ ███████╗ █████╗ ███╗   ███╗██╗███╗   ██╗ ██████╗ 
        //██╔════╝╚══██╔══╝██╔══██╗██╔════╝██╔══██╗████╗ ████║██║████╗  ██║██╔════╝ 
        //███████╗   ██║   ██████╔╝█████╗  ███████║██╔████╔██║██║██╔██╗ ██║██║  ███╗
        //╚════██║   ██║   ██╔══██╗██╔══╝  ██╔══██║██║╚██╔╝██║██║██║╚██╗██║██║   ██║
        //███████║   ██║   ██║  ██║███████╗██║  ██║██║ ╚═╝ ██║██║██║ ╚████║╚██████╔╝
        //╚══════╝   ╚═╝   ╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚═╝     ╚═╝╚═╝╚═╝  ╚═══╝ ╚═════╝ 

        /// <summary>
        /// Performs necessary operations to set up 'stream' control mode in the controller
        /// </summary>
        /// <returns></returns>
        private bool SetupStreamingMode()
        {
            throw new NotImplementedException();
        }

        public override bool SetRunMode(CycleType mode)
        {
            throw new NotImplementedException();
        }

        internal override bool ConfigureBuffer(int minActions, int maxActions)
        {
            return this._tcpManager.ConfigureBuffer(minActions, maxActions);
        }

    }
}