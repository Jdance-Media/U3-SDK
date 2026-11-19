////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System.Collections;
using UnityEngine;

namespace SDG.Unturned
{
	// Jdance 2026-11-18: I settled on a look-up table that is communicated from the server to the client.
	// Works very similarly to NetIds. The lookup table could use the original NetId struct, but it seems
	// best practice to seperate them just in case. As these are per each asset.
	/// <summary>
	/// Unsigned 32-bit integer representation of dynamic item IDs. Used for lookup on the server and client.
	/// </summary>
	///
	public struct NetItemId : System.IEquatable<NetItemId>
	{
		public static readonly NetItemId INVALID = new NetItemId(0);

		public NetItemId(uint id)
		{
			this._id = id;
		}

		/// <summary>
		/// Zero is treated as unset.
		/// </summary>
		public bool IsNull()
		{
			return _id == 0U;
		}

		public void Clear()
		{
			_id = 0;
		}

		public override bool Equals(object obj)
		{
			return obj is NetItemId && _id == ((NetItemId) obj)._id;
		}

		public bool Equals(NetItemId other)
		{
			return _id == other._id;
		}

		public override int GetHashCode()
		{
			return _id.GetHashCode();
		}

		public override string ToString()
		{
			return _id.ToString("X8"); // 8 digit hexadecimal uppercase
		}

		public static bool operator ==(NetItemId lhs, NetItemId rhs)
		{
			return lhs._id == rhs._id;
		}

		public static bool operator !=(NetItemId lhs, NetItemId rhs)
		{
			return lhs._id != rhs._id;
		}

		public static NetItemId operator +(NetItemId lhs, uint rhs)
		{
			return new NetItemId(lhs._id + rhs);
		}

		public static NetItemId operator ++(NetItemId value)
		{
			return new NetItemId(value._id + 1U);
		}

		public static NetItemId operator -(NetItemId lhs, uint rhs)
		{
			return new NetItemId(lhs._id - rhs);
		}

		public static NetItemId operator --(NetItemId value)
		{
			return new NetItemId(value._id - 1U);
		}

		internal uint _id;

		public uint Id { get => _id; }

		/// <summary>
		/// Used to have the dynamic ID system imitate legacy IDs.
		/// </summary>
		public ushort LegacyId { get => (ushort) _id; }
	}

	// Jdance 2026-11-18: PakEx class will be added later on. Right now imitation of ushort IDs doesn't need it.
}
