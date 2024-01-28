﻿#if HAS_TASK
// <copyright file="IAdbCommandLineClient.Async.cs" company="The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere">
// Copyright (c) The Android Open Source Project, Ryan Conrad, Quamotion, yungd1plomat, wherewhere. All rights reserved.
// </copyright>

using System.Threading;

namespace AdvancedSharpAdbClient
{
    public partial interface IAdbCommandLineClient
    {
        /// <summary>
        /// Asynchronously queries adb for its version and path and checks it against <see cref="AdbServer.RequiredAdbVersion"/>.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task{AdbCommandVersion}"/> which returns a <see cref="AdbCommandLineStatus"/> object that represents the version and path of the adb command line client.</returns>
        Task<AdbCommandLineStatus> GetVersionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously starts the adb server by running the <c>adb start-server</c> command.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        Task StartServerAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether the <c>adb.exe</c> file exists.
        /// </summary>
        /// <param name="adbPath">The path to validate.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task{Boolean}"/> which returns <see langword="true"/> if the <c>adb.exe</c> file is exists, otherwise <see langword="false"/>.</returns>
        Task<bool> CheckAdbFileExistsAsync(string adbPath, CancellationToken cancellationToken);
    }
}
#endif