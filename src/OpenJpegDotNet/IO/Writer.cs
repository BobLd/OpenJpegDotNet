﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenJpegDotNet.IO
{

    public sealed class Writer : IDisposable
    {

        #region Fields

        private readonly Buffer _Buffer;

        private readonly IntPtr _UserData;

        private readonly DelegateHandler<StreamWrite> _WriteCallback;

        private readonly DelegateHandler<StreamSeek> _SeekCallback;

        private readonly DelegateHandler<StreamSkip> _SkipCallback;

        private Codec _Codec;

        private CompressionParameters _CompressionParameters;

        private OpenJpegDotNet.Image _Image;

        private readonly Stream _Stream;

        #endregion

        #region Constructors

        public Writer(Bitmap bitmap)
        {
            _Image = ImageHelper.FromBitmap(bitmap);
            int datalen = (int)(_Image.X1 * _Image.Y1 * _Image.NumberOfComponents + 1024);

            this._Buffer = new Buffer
            {
                Data = Marshal.AllocHGlobal(datalen),
                Length = datalen,
                Position = 0
            };

            var size = Marshal.SizeOf(this._Buffer);
            this._UserData = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this._Buffer, this._UserData, false);

            this._WriteCallback = new DelegateHandler<StreamWrite>(Write);
            this._SeekCallback = new DelegateHandler<StreamSeek>(Seek);
            this._SkipCallback = new DelegateHandler<StreamSkip>(Skip);

            this._Stream = OpenJpeg.StreamCreate((ulong)_Buffer.Length, false);
            OpenJpeg.StreamSetUserData(this._Stream, this._UserData);
            OpenJpeg.StreamSetUserDataLength(this._Stream, this._Buffer.Length);
            OpenJpeg.StreamSetWriteFunction(this._Stream, this._WriteCallback);
            OpenJpeg.StreamSetSeekFunction(this._Stream, this._SeekCallback);
            OpenJpeg.StreamSetSkipFunction(this._Stream, this._SkipCallback);

            _Codec = OpenJpeg.CreateCompress(CodecFormat.J2k);

            this._CompressionParameters = new CompressionParameters();
            OpenJpeg.SetDefaultEncoderParameters(this._CompressionParameters);
            this._CompressionParameters.TcpNumLayers = 1;
            this._CompressionParameters.CodingParameterDistortionAllocation = 1;

            OpenJpeg.SetupEncoder(_Codec, _CompressionParameters, _Image);
        }

        #endregion

        #region Properties

        public int Height
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has been disposed.
        /// </summary>
        /// <returns>true if this instance has been disposed; otherwise, false.</returns>
        public bool IsDisposed
        {
            get;
            private set;
        }

        public int Width
        {
            get;
            private set;
        }

        #endregion

        #region Methods

        public byte[] Encode()
        {
            OpenJpeg.StartCompress(_Codec, _Image, _Stream);
            OpenJpeg.Encode(_Codec, _Stream);
            OpenJpeg.EndCompress(_Codec, _Stream);

            var datast = Marshal.PtrToStructure<Buffer>(_UserData);
            var output = new byte[datast.Position];
            Marshal.Copy(_Buffer.Data, output, 0, output.Length);

            return output;
        }

        #region Event Handlers

        private static int Seek(ulong bytes, IntPtr userData)
        {
            unsafe
            {
                var buf = (Buffer*)userData;
                var position = Math.Min((ulong)buf->Length, bytes);
                buf->Position = (int)position;
                return 1;
            }
        }

        private static long Skip(ulong bytes, IntPtr userData)
        {
            unsafe
            {
                var buf = (Buffer*)userData;
                var bytesToSkip = (int)Math.Min((ulong)buf->Length, bytes);
                if (bytesToSkip > 0)
                {
                    buf->Position += bytesToSkip;
                    return bytesToSkip;
                }
                else
                {
                    return unchecked(-1);
                }
            }
        }

        private static ulong Write(IntPtr buffer, ulong bytes, IntPtr userData)
        {
            unsafe
            {
                var buf = (Buffer*)userData;
                var bytesToRead = (int)Math.Min((ulong)buf->Length, bytes);
                if (bytesToRead > 0)
                {
                    NativeMethods.cstd_memcpy(buffer, IntPtr.Add(buf->Data, buf->Position), bytesToRead);
                    buf->Position += bytesToRead;
                    return (ulong)bytesToRead;
                }
                else
                {
                    return unchecked((ulong)-1);
                }
            }
        }

        #endregion

        #region Helpers

        public void SetupEncoderParameters(CompressionParameters cparameters)
        {
            _CompressionParameters = cparameters;
            OpenJpeg.SetupEncoder(_Codec, _CompressionParameters, _Image);
        }

        #endregion

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Releases all resources used by this <see cref="Reader"/>.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            //GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by this <see cref="Reader"/>.
        /// </summary>
        /// <param name="disposing">Indicate value whether <see cref="IDisposable.Dispose"/> method was called.</param>
        private void Dispose(bool disposing)
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;

            if (disposing)
            {
                this._Codec?.Dispose();
                this._CompressionParameters?.Dispose();
                this._Stream.Dispose();

                Marshal.FreeHGlobal(this._Buffer.Data);
                Marshal.FreeHGlobal(this._UserData);
            }
        }

        #endregion

    }

}