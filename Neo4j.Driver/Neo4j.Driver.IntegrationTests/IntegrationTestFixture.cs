﻿// Copyright (c) 2002-2016 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using Neo4j.Driver.IntegrationTests.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using Neo4j.Driver.Internal;
using Neo4j.Driver.V1;
using Xunit;
using Path = System.IO.Path;

namespace Neo4j.Driver.IntegrationTests
{
    public class IntegrationTestFixture : IDisposable
    {
        private readonly INeo4jInstaller _installer = new WindowsNeo4jInstaller();
        public string Neo4jHome { get; }

        public string ServerEndPoint => "bolt://localhost";
        public IAuthToken AuthToken { get; private set; }

        public IntegrationTestFixture()
        {
            try
            {
                _installer.DownloadNeo4j();
                _installer.InstallServer();
                _installer.StartServer();
            }
            catch
            {
                try { Dispose(); } catch { /*Do nothing*/ }
                throw;
            }
            Neo4jHome = _installer.Neo4jHome.FullName;

            // work around for the default password problem
            ChangeUserPassword("neo4j","TOUFU");
            ChangeUserPassword("TOUFU", "neo4j");
        }

        private void ChangeUserPassword(string oldPassword, string newPassword)
        {
            using (var driver = GraphDatabase.Driver(
                ServerEndPoint,
                new AuthToken(new Dictionary<string, object>
                {
                    {"scheme", "basic"},
                    {"principal", "neo4j"},
                    {"credentials", oldPassword},
                    {"new_credentials", newPassword}
                })))
            using (var session = driver.Session())
            {
                session.Run("RETURN 1 as Number").Consume();
            }

            AuthToken = AuthTokens.Basic("neo4j", newPassword);
        }

        public void RestartServerWithUpdatedSettings(IDictionary<string, string> keyValuePair)
        {
            try
            {
                _installer.StopServer();
                _installer.UpdateSettings(keyValuePair);
                _installer.StartServer();
            }
            catch
            {
                try { Dispose(); } catch { /*Do nothing*/ }
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _installer.StopServer();
            }
            catch
            {
                // ignored
            }
            _installer.UninstallServer();
        }
    }

    [CollectionDefinition(CollectionName)]
    public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture>
    {
        public const string CollectionName = "Integration";
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    public static class Extensions
    {
        public static float BytesToMegabytes(this long bytes)
        {
            return bytes/1024f/1024f;
        }
    }
}