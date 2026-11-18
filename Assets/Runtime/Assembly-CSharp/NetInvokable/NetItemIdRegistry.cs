////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SDG.Unturned
{
	public static class NetItemIdRegistry
	{
		/// <summary>
		/// Increments the server counter and reserves a NetItemId.
		/// </summary>
		public static NetItemId Claim()
		{
			return new NetItemId(++_counter);
		}

		/// <summary>
		/// Increments the server counter by the size and reserves a range of NetItemIds.
		/// </summary>
		public static NetItemId ClaimBlock(uint size)
		{
			NetItemId result = new NetItemId(_counter + 1);
			_counter += size;
			return result;
		}

		public static Asset Get(NetItemId key)
		{
			Asset value;
			_idPairings.TryGetValue(key, out value);
			return value;
		}

		public static T GetAsset<T>(NetItemId key) where T : Asset
		{
			Asset value = Get(key);
			// Prevent exception.
			return value == null ? null : value as T;
		}

		public static NetItemId GetId(Guid assetGuid)
		{
			NetItemId value;
			return _assetPairings.TryGetValue(assetGuid, out value) ? value : NetItemId.INVALID;
		}

		public static NetItemId GetId(Asset asset)
		{
			return GetId(asset.GUID);
		}

		public static void Clear()
		{
			_counter = 0;
			_idPairings.Clear();
			_assetPairings.Clear();
		}

		public static bool Release(NetItemId key)
		{
			Asset asset;
			bool result = _idPairings.TryGetValue(key, out asset);
			_idPairings.Remove(key);
			if (result)
				_assetPairings.Remove(asset.GUID, out _);
			Log(result ? $"Released NetItemID {key}" : $"Released unused NetItemID {key}");
			return result;
		}

		// Jdance 2026-11-18: I will keep this under the same compiler flag for now. Not sure if a
		// LOG_NET_ITEM_ID flag is necessary.
		[System.Diagnostics.Conditional("LOG_NET_ID")]
		private static void Log(string message)
		{
			UnturnedLog.info(message);
		}

		private static uint _counter;

		private static Dictionary<NetItemId, Asset> _idPairings = new Dictionary<NetItemId, Asset>();

		private static Dictionary<Guid, NetItemId> _assetPairings = new Dictionary<Guid, NetItemId>();
	}
}
