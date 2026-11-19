////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using SDG.Provider;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned
{
	public interface IStateConversion
	{
		public byte[] ConvertStateToSavedata(byte[] state);

		public byte[] ConvertStateFromSavedata(byte[] state);
	}
}
