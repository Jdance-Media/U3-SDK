////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the U3 SDK: https://github.com/smartlydressedgames/u3-sdk/    //
// Please refer to the included LICENSE.txt for copyright notice and license details. //
////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Buffers.Binary;
using System.IO;
using Unturned.SystemEx;

namespace SDG.Unturned
{
	public class ItemStorageAsset : ItemBarricadeAsset, IStateConversion
	{
		protected byte _storage_x;
		public byte storage_x => _storage_x;

		protected byte _storage_y;
		public byte storage_y => _storage_y;

		protected bool _isDisplay;
		public bool isDisplay => _isDisplay;

		public bool shouldCloseWhenOutsideRange
		{
			get;
			protected set;
		}

		/// <summary>
		/// If true, players can interact with the barricade to open storage.
		/// Defaults to true.
		/// Useful for pre-placed sentries to prevent stealing their guns.
		/// </summary>
		public bool CanPlayersOpen
		{
			get;
			set;
		}

		/// <summary>
		/// If true, any stored items are despawned rather than dropped when destroyed.
		/// Defaults to false.
		/// </summary>
		public bool ShouldDeleteContainedItemsOnDestroy
		{
			get;
			set;
		}

		public LevelAsset.DefaultLoadoutItem[] DefaultContainedItems
		{
			get;
			set;
		}

		public void AddDefaultContainedItemsToStorage(InteractableStorage storage)
		{
			if (storage == null || DefaultContainedItems.IsNullOrEmpty())
			{
				return;
			}

			foreach (LevelAsset.DefaultLoadoutItem item in DefaultContainedItems)
			{
				ItemAsset itemAsset = item.ResolveAsset(OnGetDefaultContainedItemsErrorContext);
				if (itemAsset == null)
				{
					continue;
				}

				for (int amount = 0; amount < item.amount; ++amount)
				{
					storage.items.tryAddItem(new Item(itemAsset, item.origin), false);
				}
			}

			storage.items.onStateUpdated?.Invoke();
		}

		private string OnGetDefaultContainedItemsErrorContext()
		{
			return $"{FriendlyNameWithFriendlyType} default contained items";
		}

		public override byte[] getState(EItemOrigin origin)
		{
			if (isDisplay)
			{
				return new byte[21];
			}
			else
			{
				return new byte[17];
			}
		}

		public override void BuildDescription(ItemDescriptionBuilder builder, Item itemInstance)
		{
			base.BuildDescription(builder, itemInstance);

			if (!builder.HasFlag(EItemDescriptionFlags.Uncategorized))
				return;

			if (storage_x > 0 && storage_y > 0)
			{
				builder.Append(PlayerDashboardInventoryUI.localization.format("ItemDescription_StorageDimensions", storage_x, storage_y), DescSort_Important);
			}
		}

		public override void PopulateAsset(in PopulateAssetParameters p)
		{
			base.PopulateAsset(in p);

			_storage_x = p.data.ParseUInt8("Storage_X");
			if (storage_x < 1)
			{
				_storage_x = 1;
			}

			_storage_y = p.data.ParseUInt8("Storage_Y");
			if (storage_y < 1)
			{
				_storage_y = 1;
			}

			_isDisplay = p.data.ContainsKey("Display");
			shouldCloseWhenOutsideRange = p.data.ParseBool("Should_Close_When_Outside_Range", defaultValue: false);
			CanPlayersOpen = p.data.ParseBool("Can_Players_Open", true);
			ShouldDeleteContainedItemsOnDestroy = p.data.ParseBool("Delete_Contained_Items_On_Destroy");

			if (p.data.TryGetList("Default_Contained_Items", out IDatList itemsNode))
			{
				DefaultContainedItems = itemsNode.ParseArrayOfStructs<LevelAsset.DefaultLoadoutItem>();
			}
		}

		internal override void BuildCargoData(CargoBuilder builder)
		{
			base.BuildCargoData(builder);

			// https://unturned.wiki.gg/wiki/Special:CargoTables/Storage
			// Game data for Storage Item assets.
			CargoDeclaration data = builder.GetOrAddDeclaration("Storage");
			data.Append("GUID", GUID); // Key

			data.Append("Storage_X", storage_x);
			data.Append("Storage_Y", storage_y);
			data.Append("Display", isDisplay);
			data.Append("Should_Close_When_Outside_Range", shouldCloseWhenOutsideRange);
		}

		public new byte[] ConvertStateToSavedata(byte[] state)
		{
			if (state == null || state.Length < 17)
			{
				return state;
			}

			ReadOnlySpan<byte> stateSpan = state;
			Span<byte> guidBytes = stackalloc byte[16];
			byte count = state[16];
			// This is agruably the best way to deal with the variable state size for now.
			using (MemoryStream stream = new MemoryStream(state.Length + count * 14))
			{
				stream.Write(stateSpan[..17]);

				int offset = 17;
				for (byte index = 0; index < count; index++)
				{
					// x, y, and rot
					stream.Write(stateSpan.Slice(offset, 3));

					NetItemId netItemId = new NetItemId(BinaryPrimitives.ReadUInt16LittleEndian(stateSpan.Slice(offset + 3, 2)));
					ItemAsset itemAsset = NetItemIdRegistry.Get(netItemId);
					Guid itemGuid = itemAsset != null ? itemAsset.GUID : Guid.Empty;
					itemGuid.TryWriteBytes(guidBytes);
					stream.Write(guidBytes);

					// amount, and quality
					stream.Write(stateSpan.Slice(offset + 5, 2));

					byte itemStateLength = state[offset + 7];
					byte[] itemState = stateSpan.Slice(offset + 8, itemStateLength).ToArray();
					stream.WriteByte((byte) itemState.Length);
					stream.Write(itemState, 0, itemState.Length);

					offset += 8 + itemStateLength;
				}

				// Rest of the data that offset by the variable size.
				stream.Write(stateSpan[offset..]);

				return stream.ToArray();
			}
		}

		public new byte[] ConvertStateFromSavedata(byte[] state)
		{
			if (state == null || state.Length < 17)
			{
				return state;
			}

			ReadOnlySpan<byte> savedataSpan = state;
			Span<byte> idBytes = stackalloc byte[2];
			using (MemoryStream stream = new MemoryStream(state.Length))
			{
				stream.Write(savedataSpan[..17]);

				byte count = state[16];
				int offset = 17;
				for (byte index = 0; index < count; index++)
				{
					// x, y, rot
					stream.Write(savedataSpan.Slice(offset, 3));

					// InteractableStorage.updateState *should* skip the invalid ID (0).
					NetItemId netItemId = NetItemIdRegistry.GetId(new Guid(savedataSpan.Slice(offset + 3, 16)));
					ItemAsset itemAsset = NetItemIdRegistry.Get(netItemId);
					BinaryPrimitives.WriteUInt16LittleEndian(idBytes, netItemId.LegacyId);
					stream.Write(idBytes);

					// amount, quality
					stream.Write(savedataSpan.Slice(offset + 19, 2));

					byte itemSavedataStateLength = state[offset + 21];
					byte[] itemSavedataState = savedataSpan.Slice(offset + 22, itemSavedataStateLength).ToArray();
					stream.WriteByte((byte) itemSavedataState.Length);
					stream.Write(itemSavedataState, 0, itemSavedataState.Length);

					offset += 22 + itemSavedataStateLength;
				}

				// This write lasts for however long the rest of the state is. If the asset later becomes a
				// display or vice-versa, the modder is technically at fault for anything bad that occurs.
				stream.Write(savedataSpan[offset..]);

				return stream.ToArray();
			}
		}
	}
}
