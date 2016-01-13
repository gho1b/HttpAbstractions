// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Extensions
{
    // FYI: In most cases the source will be a FileStream and the destination will be to the network.
    public static class StreamCopyOperation
    {
        private const int DefaultBufferSize = 4096;

        public static async Task CopyToAsync(Stream source, Stream destination, long? length, CancellationToken cancel)
        {
            long? bytesRemaining = length;

            var buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
            try
            {
                Debug.Assert(source != null);
                Debug.Assert(destination != null);
                Debug.Assert(!bytesRemaining.HasValue || bytesRemaining.Value >= 0);
                Debug.Assert(buffer != null);

                while (true)
                {
                    // The natural end of the range.
                    if (bytesRemaining.HasValue && bytesRemaining.Value <= 0)
                    {
                        return;
                    }

                    cancel.ThrowIfCancellationRequested();

                    int readLength = buffer.Length;
                    if (bytesRemaining.HasValue)
                    {
                        readLength = (int)Math.Min(bytesRemaining.Value, (long)readLength);
                    }
                    int count = await source.ReadAsync(buffer, 0, readLength, cancel);

                    if (bytesRemaining.HasValue)
                    {
                        bytesRemaining -= count;
                    }

                    // End of the source stream.
                    if (count == 0)
                    {
                        return;
                    }

                    cancel.ThrowIfCancellationRequested();

                    await destination.WriteAsync(buffer, 0, count, cancel);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}