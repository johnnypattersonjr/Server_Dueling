// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsStatManagerSO()
{
	if (isObject(dsStatManagerSO))
		return nameToID(dsStatManagerSO);

	%obj = new ScriptObject(dsStatManagerSO)
	{
		dirtyCheckPeriodMs = 10000;
		recordDirectory = "config/server/dueling/records/";
	};

	%obj.dirtyRecordList = new GuiTextListCtrl();
	%obj.recordGroup = new SimGroup();
	%obj.dirtyCheck();

	return %obj;
}

function dsStatManagerSO::addDuel(%this, %weapon, %winnerRecord, %loserRecord)
{
	%itemName = %weapon.getName();

	%winnerRecord.duels++;
	%winnerRecord.duels[%itemName]++;
	%winnerRecord.wins++;
	%winnerRecord.wins[%itemName]++;
	%winnerRecord.losses[%itemName] *= 1;
	%loserRecord.duels++;
	%loserRecord.duels[%itemName]++;
	%loserRecord.losses++;
	%loserRecord.losses[%itemName]++;
	%loserRecord.wins[%itemName] *= 1;
	%winnerRecord.dirty = 1;
	%loserRecord.dirty = 1;

	%list = %this.dirtyRecordList;
	%list.setRowById(%winnerRecord, 1);
	%list.setRowById(%loserRecord, 1);
}

function dsStatManagerSO::addRound(%this, %weapon, %winnerRecord, %loserRecord, %draw)
{
	%itemName = %weapon.getName();

	if (%draw)
	{
		%winnerRecord.deaths++;
		%winnerRecord.deaths[%itemName]++;
		%winnerRecord.kills[%itemName] *= 1;
		%loserRecord.deaths++;
		%loserRecord.deaths[%itemName]++;
		%loserRecord.kills[%itemName] *= 1;
	}
	else
	{
		%winnerRecord.kills++;
		%winnerRecord.kills[%itemName]++;
		%winnerRecord.deaths[%itemName] *= 1;
		%loserRecord.deaths++;
		%loserRecord.deaths[%itemName]++;
		%loserRecord.kills[%itemName] *= 1;
	}

	%winnerRecord.dirty = 1;
	%loserRecord.dirty = 1;
}

function dsStatManagerSO::dirtyCheck(%this)
{
	cancel(%this.dirtyCheckEvent);

	%list = %this.dirtyRecordList;
	%count = %list.rowCount();
	for (%i = 0; %i < %count; %i++)
	{
		%record = %list.getRowId(%i);
		%record.dirty = "";
		%record.save(%this.recordDirectory @ %record.bl_id @ ".cs");
	}
	%list.clear();

	%this.dirtyCheckEvent = %this.schedule(%this.dirtyCheckPeriodMs, dirtyCheck);
}

function dsStatManagerSO::getRecordFromClient(%this, %client, %create)
{
	%record = "DuelingRecord_" @ %client.bl_id;

	if (isObject(%record))
	{
		%record = nameToID(%record);
	}
	else
	{
		%group = %this.recordGroup;
		%path = %this.recordDirectory @ %client.bl_id @ ".cs";

		%instantGroupBackup = $instantGroup;
		$instantGroup = %group;

		if (isFile(%path))
		{
			exec(%path);
			%record = %group.getObject(%group.getCount() - 1);
		}
		else if (%create)
		{
			%record = new ScriptObject(DuelingRecord_ @ %client.bl_id)
			{
				class = dsStatRecord;
				bl_id = %client.bl_id;
				name = %client.name;
				deaths = 0;
				duels = 0;
				kills = 0;
				losses = 0;
				wins = 0;
			};

			%record.snapshotAvatar(%client);
		}
		else
		{
			%record = 0;
		}

		$instantGroup = %instantGroupBackup;
	}

	return %record;
}

function dsStatManagerSO::getRecordFromID(%this, %bl_id, %create)
{
	%record = "DuelingRecord_" @ %bl_id;

	if (isObject(%record))
	{
		%record = nameToID(%record);
	}
	else
	{
		%group = %this.recordGroup;
		%path = %this.recordDirectory @ %bl_id @ ".cs";

		%instantGroupBackup = $instantGroup;
		$instantGroup = %group;

		if (isFile(%path))
		{
			exec(%path);
			%record = %group.getObject(%group.getCount() - 1);
		}
		else if (%create)
		{
			%record = new ScriptObject(DuelingRecord_ @ %bl_id)
			{
				class = dsStatRecord;
				bl_id = %bl_id;
				name = "Unknown";
				deaths = 0;
				duels = 0;
				kills = 0;
				losses = 0;
				wins = 0;
			};

			%client = findClientByBL_ID(%bl_id);
			if (isObject(%client))
				%record.snapshotAvatar(%client);
		}
		else
		{
			%record = 0;
		}

		$instantGroup = %instantGroupBackup;
	}

	return %record;
}

function dsStatRecord::snapshotAvatar(%this, %client)
{
	%this.DecalName = %client.DecalName;
	%this.FaceName = %client.FaceName;

	%this.AccentColor = %client.AccentColor;
	%this.ChestColor = %client.ChestColor;
	%this.HatColor = %client.HatColor;
	%this.HeadColor = %client.HeadColor;
	%this.HipColor = %client.HipColor;
	%this.LArmColor = %client.LArmColor;
	%this.LHandColor = %client.LHandColor;
	%this.LLegColor = %client.LLegColor;
	%this.PackColor = %client.PackColor;
	%this.RArmColor = %client.RArmColor;
	%this.RHandColor = %client.RHandColor;
	%this.RLegColor = %client.RLegColor;
	%this.SecondPackColor = %client.SecondPackColor;

	%this.Accent = %client.Accent;
	%this.Chest = %client.Chest;
	%this.Hat = %client.Hat;
	%this.Hip = %client.Hip;
	%this.LArm = %client.LArm;
	%this.LHand = %client.LHand;
	%this.LLeg = %client.LLeg;
	%this.Pack = %client.Pack;
	%this.RArm = %client.RArm;
	%this.RHand = %client.RHand;
	%this.RLeg = %client.RLeg;
	%this.SecondPack = %client.SecondPack;

	%this.dirty = 1;
	%list = dsStatManagerSO.dirtyRecordList;
	%list.setRowById(%this, 1);
}

function serverCmdUpdateBodyParts(%client, %arg1, %arg2, %arg3, %arg4, %arg5, %arg6, %arg7, %arg8, %arg9, %arg10, %arg11, %arg12)
{
	Parent::serverCmdUpdateBodyParts(%client, %arg1, %arg2, %arg3, %arg4, %arg5, %arg6, %arg7, %arg8, %arg9, %arg10, %arg11, %arg12);

	if (%client.avatarSaved)
		return;

	%record = dsStatManagerSO.getRecordFromClient(%client, 1);
	%record.snapshotAvatar(%client);
	%client.avatarSaved = 1;
}

}; // package Server_Dueling
