// Copyright 2020 New Relic, Inc. All rights reserved.
// SPDX-License-Identifier: Apache-2.0


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace NewRelic.Agent.IntegrationTestHelpers
{
    public static class RandomPortGenerator
    {
        private const int minPortID = 50000;
        private const int maxPortID = 60000;
        private const int portPoolSize = maxPortID - minPortID;
        private const int maxAttempts = 200;

        private static int _currentPortID;

        static RandomPortGenerator()
        {
            var seed = Process.GetCurrentProcess().Id + AppDomain.CurrentDomain.Id + Environment.TickCount;
            var rnd = new Random(seed);
            _currentPortID = rnd.Next(portPoolSize);
        }

        public static int NextPort()
        {
            for (var countAttempts = 0; countAttempts < maxAttempts; countAttempts++)
            {
                var potentialPort = (Interlocked.Increment(ref _currentPortID) % portPoolSize) + minPortID;
                if (IsPortAvailable(potentialPort))
                {
                    return potentialPort;
                }
            }

            throw new Exception($"Unable to obtain port after {maxAttempts} attempts.");
        }


        //Checks if something outside our current test run instance is currently using the port.
        //This does not prevent us from getting into a conflict with another process taking that port after this check,
        //but before the test app uses the assigned port.
        private static bool IsPortAvailable(int potentialPort)
        {
            var tcpListener = new TcpListener(System.Net.IPAddress.Any, potentialPort);
            try
            {
                tcpListener.Start();
                // we got the port, so can return (implicitly closes listener using finally block)
                return true;
            }
            catch (Exception)
            {
                // we were unable to get the port
                return false;
            }
            finally
            {
                try
                {
                    tcpListener.Stop();
                }
                catch (Exception)
                {
                    // Ignore errors stopping the listener
                }
            }
        }

        public static bool TryReleasePort(int port)
        {
            return true;
        }
    }
}
