using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Net.Middleware.Modbus
{
	public class PointEventArgs<T> : PointEventArgs where T : struct
	{
		private readonly T[] _points;

		public PointEventArgs(ushort startAddress, T[] points) : base(startAddress, (ushort)points.Length)
		{
			_points = points;
		}

		public T[] Points => _points;
	}
	/// <summary>
	/// Modbus Slave request event args containing information on the message.
	/// </summary>
	public class PointEventArgs : EventArgs
	{
		private ushort _numberOfPoints;

		private ushort _startAddress;

		public PointEventArgs(ushort startAddress, ushort numberOfPoints)
		{
			_startAddress = startAddress;
			_numberOfPoints = numberOfPoints;
		}

		public ushort NumberOfPoints => _numberOfPoints;

		public ushort StartAddress => _startAddress;
	}
}
