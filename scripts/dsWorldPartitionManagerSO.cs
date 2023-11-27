// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsWorldPartitionManagerSO()
{
	if (isObject(dsWorldPartitionManagerSO))
		return nameToID(dsWorldPartitionManagerSO);

	%obj = new ScriptObject(dsWorldPartitionManagerSO)
	{
	};

	%obj.partitions = new SimGroup();

	return %obj;
}

function dsWorldPartitionManagerSO::allocate(%this)
{
	%obj = new ScriptObject()
	{
		class = dsWorldPartitionSO;
	};

	%this.partitions.add(%obj);

	return %obj;
}

function dsWorldPartitionSO::hostBuildingSession(%this, %client)
{
	if (isObject(%this.miniGame))
		return 0;

	// TODO: Handle bricks owned by players who aren't part of the current session.

	%colorIdx = 2; // Yellow
	$MiniGameColorTaken[%colorIdx] = 0;
	%miniGame = CreateMiniGameSO(%client, "Build Session", %colorIdx, 1);
	%miniGame.InviteOnly = 1;
	%miniGame.buildSession = 1;
	%miniGame.EnableBuilding = 1;
	%miniGame.EnablePainting = 1;
	%miniGame.EnableWand = 1;
	%miniGame.partition = %this;
	%miniGame.SelfDamage = 1;
	%miniGame.StartEquip0 = nameToID(HammerItem);
	%miniGame.StartEquip1 = nameToID(WrenchItem);
	%miniGame.StartEquip2 = nameToID(PrintGun);
	%miniGame.WeaponDamage = 1;
	%this.miniGame = %miniGame;

	commandToClient(%client, 'dcCloseWindow');
	commandToClient(%client, 'dcSetBuilding', 2);
	commandToClient(%client, 'dcSetBuildSessionMember', %client, %client.name);

	messageClient(%client, '', '\c5Mini-game created.');
	commandToClient(%client, 'SetPlayingMiniGame', 1);
	commandToClient(%client, 'SetRunningMiniGame', 1);
	commandToClient(%client, 'SetBuildingDisabled', 0);
	commandToClient(%client, 'SetPaintingDisabled', 0);
	commandToAll('AddMiniGameLine', %miniGame.getLine(), %miniGame, %colorIdx);

	%miniGame.Reset(%client);

	return 1;
}

function dsWorldPartitionSO::hostDuelingSession(%this, %duelist1, %duelist2, %weapon, %goal, %practice)
{
	if (isObject(%this.miniGame))
		return 0;

	// TODO: Handle bricks owned by players who aren't part of the current session.

	%ranked = !%practice;

	%colorIdx = 0; // Red
	$MiniGameColorTaken[%colorIdx] = 0;
	%miniGame = CreateMiniGameSO(%duelist1, "Duel", %colorIdx, 1);
	%miniGame.applyDuelingSettings(%weapon);
	%miniGame.InviteOnly = 0;
	%miniGame.duelist1 = %duelist1;
	%miniGame.duelist2 = %duelist2;
	%miniGame.addMember(%duelist2);
	%miniGame.duelistScore1 = 0;
	%miniGame.duelistScore2 = 0;
	%miniGame.duelistName1 = %duelist1.name;
	%miniGame.duelistName2 = %duelist2.name;
	%miniGame.duel = 1;
	%miniGame.goal = %goal;
	%miniGame.partition = %this;
	%miniGame.ranked = %ranked;

	if (%ranked)
	{
		%miniGame.duelistRecord1 = dsStatManagerSO.getRecordFromClient(%duelist1, 1);
		%miniGame.duelistRecord2 = dsStatManagerSO.getRecordFromClient(%duelist2, 1);
	}

	%this.miniGame = %miniGame;

	if (!%duelist1.isAIControlled())
	{
		messageClient(%duelist1, '', '\c5Mini-game created.');
		commandToClient(%duelist1, 'dcCloseWindow');
		commandToClient(%duelist1, 'dcSetDueling', 1);
		commandToClient(%duelist1, 'SetPlayingMiniGame', 1);
		commandToClient(%duelist1, 'SetRunningMiniGame', 0);
	}

	if (!%duelist2.isAIControlled())
	{
		messageClient(%duelist2, '', '\c5Mini-game created.');
		commandToClient(%duelist2, 'dcCloseWindow');
		commandToClient(%duelist2, 'dcSetDueling', 1);
		commandToClient(%duelist2, 'SetPlayingMiniGame', 1);
		commandToClient(%duelist2, 'SetRunningMiniGame', 0);
	}

	%miniGame.commandToAll('SetBuildingDisabled', 1);
	%miniGame.commandToAll('SetPaintingDisabled', 1);

	commandToAll('AddMiniGameLine', %miniGame.getLine(), %miniGame, %colorIdx);

	%miniGame.Reset(%duelist1);

	return 1;
}

function dsWorldPartitionSO::onRemove(%this)
{
	if (isObject(%this.miniGame))
	{
		%this.miniGame.partition = "";
		if (!%this.miniGame.ending)
			%this.miniGame.endGame();
		%this.miniGame.delete();
	}
}

function MiniGameSO::addMember(%this, %client)
{
	Parent::addMember(%this, %client);

	if (%client.miniGame != %this)
		return;

	// Will always be false for the owner when using CreateMinigameSO.
	if (%this.buildSession)
	{
		%this.commandToAll('dcSetBuildSessionMember', %client, %client.name);

		if (!%client.isAIControlled())
			commandToClient(%client, 'dcSetBuilding', 1);
	}
	else if (%this.duel)
	{
		if (!%client.isAIControlled())
			commandToClient(%client, 'dcSetDueling', 1);

		if (%this.duelist1 != %client && %this.duelist2 != %client)
		{
			// Trigger spectator mode on player spawn attempt.
			%client.InstantRespawn();
		}
	}
}

function MiniGameSO::applyDuelingSettings(%this, %weapon)
{
	%this.BotDamage = 1;
	%this.BotRespawnTime = 5000;
	%this.BrickDamage = 0;
	%this.BrickRespawnTime = 1;
	%this.EnableBuilding = 0;
	%this.EnablePainting = 0;
	%this.EnableWand = 0;
	%this.FallingDamage = 1;
	%this.PlayerDataBlock = nameToID(PlayerNoJet);
	%this.PlayersUseOwnBricks = 0;
	%this.Score_BreakBrick = 0;
	%this.Score_Die = 0;
	%this.Score_KillBot = 0;
	%this.Score_KillPlayer = 0;
	%this.Score_KillSelf = 0;
	%this.Score_PlantBrick = 0;
	%this.RespawnTime = -1;
	%this.SelfDamage = 1;
	%this.StartEquip0 = %weapon;
	%this.StartEquip1 = 0;
	%this.StartEquip2 = 0;
	%this.StartEquip3 = 0;
	%this.StartEquip4 = 0;
	%this.TimeLimit = 0;
	%this.UseAllPlayersBricks = 1;
	%this.UseSpawnBricks = 1;
	%this.VehicleDamage = 1;
	%this.VehicleRespawnTime = 1;
	%this.WeaponDamage = 1;
}

function MiniGameSO::callResults(%this, %leaving)
{
	switch (%this.duelResult)
	{
	case 1:
		%this.duelistScore1 += 1;
		%this.chatMessageAll(0, "\c4" @ %this.duelistName1 @ " Won the Round [" @ %this.duelistScore1 @ " - " @ %this.duelistScore2 @ "]");

		if (%this.ranked)
			dsStatManagerSO.addRound(%this.StartEquip0, %this.duelistRecord1, %this.duelistRecord2);
	case 2:
		%this.duelistScore2 += 1;
		%this.chatMessageAll(0, "\c4" @ %this.duelistName2 @ " Won the Round [" @ %this.duelistScore1 @ " - " @ %this.duelistScore2 @ "]");

		if (%this.ranked)
			dsStatManagerSO.addRound(%this.StartEquip0, %this.duelistRecord2, %this.duelistRecord1);
	case 3:
		%this.duelistScore1 += 1;
		%this.duelistScore2 += 1;
		%this.chatMessageAll(0, "\c4Draw Round [" @ %this.duelistScore1 @ " - " @ %this.duelistScore2 @ "]");

		if (%this.ranked)
			dsStatManagerSO.addRound(%this.StartEquip0, %this.duelistRecord1, %this.duelistRecord2, 1);
	}

	%end = 0;

	if (%this.duelResult != 3 || isObject(%leaving))
	{
		if (%this.duelist2 == %leaving || (%this.suddenDeath && %this.duelResult == 1) || (!%this.suddenDeath && %this.duelistScore1 >= %this.goal && (%this.duelistScore1 - %this.duelistScore2) >= dsChallengeManagerSO.winBy))
		{
			if (%this.ranked)
				dsStatManagerSO.addDuel(%this.StartEquip0, %this.duelistRecord1, %this.duelistRecord2);

			%this.chatMessageAll(0, "\c4" @ %this.duelistName1 @ " Won the Duel [" @ %this.duelistScore1 @ " - " @ %this.duelistScore2 @ "]");
			%end = 1;
		}
		else if (%this.duelist1 == %leaving || (%this.suddenDeath && %this.duelResult == 2) || (!%this.suddenDeath && %this.duelistScore2 >= %this.goal && (%this.duelistScore2 - %this.duelistScore1) >= dsChallengeManagerSO.winBy))
		{
			if (%this.ranked)
				dsStatManagerSO.addDuel(%this.StartEquip0, %this.duelistRecord2, %this.duelistRecord1);

			%this.chatMessageAll(0, "\c4" @ %this.duelistName2 @ " Won the Duel [" @ %this.duelistScore1 @ " - " @ %this.duelistScore2 @ "]");
			%end = 1;
		}
	}

	if (!%end && !%this.suddenDeath && (%this.duelistScore1 == dsChallengeManagerSO.suddenDeathScore || %this.duelistScore2 == dsChallengeManagerSO.suddenDeathScore))
	{
		%this.chatMessageAll(0, "\c4Sudden Death started! Duel is decided by next round win.");
		%this.suddenDeath = 1;
	}
	else if (%end)
	{
		if (isObject(%this.duelist1) && !%this.duelist1.isAIControlled())
			commandToClient(%this.duelist1, 'dcRefreshStats');
		if (isObject(%this.duelist2) && !%this.duelist2.isAIControlled())
			commandToClient(%this.duelist2, 'dcRefreshStats');

		%weaponIndex = dsWeaponManagerSO.getWeaponIndex(%this.StartEquip0);
		if (%weaponIndex >= 0)
			commandToAll('dcRefreshRankings', %weaponIndex + 1);
	}

	%this.BottomPrintAll("", 0, 1);
	%this.duelResult = "";

	return %end;
}

function MiniGameSO::checkLastManStanding(%this)
{
	if (%this.duel)
	{
		if (%this.checkResults())
		{
			if (%this.suddenDeath)
			{
				%this.reset(0);
			}
			else
			{
				cancel(%this.resetSchedule);
				%this.scheduleReset();
			}
		}

		return;
	}

	Parent::checkLastManStanding(%this);
}

function MiniGameSO::checkResults(%this, %leaving)
{
	%duelist1 = %this.duelist1;
	%duelist2 = %this.duelist2;

	%player1Dead = !isObject(%duelist1) || !isObject(%duelist1.player) || %duelist1 == %leaving;
	%player2Dead = !isObject(%duelist2) || !isObject(%duelist2.player) || %duelist2 == %leaving;

	if (%player1Dead && %player2Dead)
	{
		%this.duelResult = 3;
		%this.BottomPrintAll("\c4Draw", 5, 1);
	}
	else if (%player2Dead)
	{
		%this.duelResult = 1;
		%this.BottomPrintAll("\c4" @ %this.duelistName1 @ " Won", 5, 1);
	}
	else if (%player1Dead)
	{
		%this.duelResult = 2;
		%this.BottomPrintAll("\c4" @ %this.duelistName2 @ " Won", 5, 1);
	}
	else
	{
		return 0;
	}

	return 1;
}

function MiniGameSO::endGame(%this)
{
	if (%this.buildSession)
	{
		%this.commandToAll('dcSetBuilding', 0);
	}
	else if (%this.duel)
	{
		%this.commandToAll('dcSetDueling', 0);
	}

	Parent::endGame(%this);

	if (isObject(%this.partition))
	{
		%this.partition.miniGame = "";
		%this.partition.delete();
	}
}

function MiniGameSO::removeMember(%this, %client)
{
	if (%this.buildSession)
	{
		if (%this.owner != %client)
		{
			if (!%client.isAIControlled())
				commandToClient(%client, 'dcSetBuilding', 0);

			%this.commandToAllExcept(%client, 'dcRemoveBuildSessionMember', %client);
		}
	}
	else if (%this.duel && !%client.isAIControlled())
	{
		commandToClient(%client, 'dcSetDueling', 0);
	}

	if (%this.duel && (%this.duelist1 == %client || %this.duelist2 == %client))
	{
		if (%this.checkResults(%client))
			%this.callResults(%client);

		if (%this.owner != %client)
			%this.stopDuel(%this.owner);
	}

	Parent::removeMember(%this, %client);
}

function MiniGameSO::reset(%this, %client)
{
	if (!%client && %this.duel)
	{
		%end = %this.callResults();

		if (%end)
		{
			%this.stopDuel(%this.owner);
			return;
		}
	}

	Parent::reset(%this, %client);

	if (isObject(%this.duelist1))
		%this.duelist1.setScore(%this.duelistScore1);
	if (isObject(%this.duelist2))
		%this.duelist2.setScore(%this.duelistScore2);
}

function MiniGameSO::startTestDuel(%this, %client, %duelist1, %duelist2, %weapon, %goal)
{
	// Snapshot existing miniGame settings.
	%this.snapshotBotDamage = %this.BotDamage;
	%this.snapshotBotRespawnTime = %this.BotRespawnTime;
	%this.snapshotBrickDamage = %this.BrickDamage;
	%this.snapshotBrickRespawnTime = %this.BrickRespawnTime;
	%this.snapshotEnableBuilding = %this.EnableBuilding;
	%this.snapshotEnablePainting = %this.EnablePainting;
	%this.snapshotEnableWand = %this.EnableWand;
	%this.snapshotFallingDamage = %this.FallingDamage;
	%this.snapshotPlayerDataBlock = %this.PlayerDataBlock;
	%this.snapshotPlayersUseOwnBricks = %this.PlayersUseOwnBricks;
	%this.snapshotScore_BreakBrick = %this.Score_BreakBrick;
	%this.snapshotScore_Die = %this.Score_Die;
	%this.snapshotScore_KillBot = %this.Score_KillBot;
	%this.snapshotScore_KillPlayer = %this.Score_KillPlayer;
	%this.snapshotScore_KillSelf = %this.Score_KillSelf;
	%this.snapshotScore_PlantBrick = %this.Score_PlantBrick;
	%this.snapshotRespawnTime = %this.RespawnTime;
	%this.snapshotSelfDamage = %this.SelfDamage;
	%this.snapshotStartEquip0 = %this.StartEquip0;
	%this.snapshotStartEquip1 = %this.StartEquip1;
	%this.snapshotStartEquip2 = %this.StartEquip2;
	%this.snapshotStartEquip3 = %this.StartEquip3;
	%this.snapshotStartEquip4 = %this.StartEquip4;
	%this.snapshotTimeLimit = %this.TimeLimit;
	%this.snapshotUseAllPlayersBricks = %this.UseAllPlayersBricks;
	%this.snapshotUseSpawnBricks = %this.UseSpawnBricks;
	%this.snapshotVehicleDamage = %this.VehicleDamage;
	%this.snapshotVehicleRespawnTime = %this.VehicleRespawnTime;
	%this.snapshotWeaponDamage = %this.WeaponDamage;

	%this.applyDuelingSettings(%weapon);
	%this.duel = 1;
	%this.testDuel = 1;
	%this.duelist1 = %duelist1;
	%this.duelist2 = %duelist2;
	%this.duelistName1 = %duelist1.name;
	%this.duelistName2 = %duelist2.name;
	%this.duelistScore1 = 0;
	%this.duelistScore2 = 0;
	%this.goal = %goal;

	%this.lastResetTime = 0;
	%this.Reset(%client);

	if (!%duelist1.isAIControlled())
		commandToClient(%duelist1, 'dcCloseWindow');
	if (!%duelist2.isAIControlled())
		commandToClient(%duelist2, 'dcCloseWindow');
	if (!%client.isAIControlled())
		commandToClient(%client, 'dcSetTestDuelStatus', 1);
}

function MiniGameSO::stopDuel(%this, %client)
{
	if (!%this.testDuel)
	{
		if (!%this.ending)
			%this.endGame();
		%this.delete();

		return;
	}

	// Restore snapshot of miniGame settings.
	%this.BotDamage = %this.snapshotBotDamage;
	%this.BotRespawnTime = %this.snapshotBotRespawnTime;
	%this.BrickDamage = %this.snapshotBrickDamage;
	%this.BrickRespawnTime = %this.snapshotBrickRespawnTime;
	%this.EnableBuilding = %this.snapshotEnableBuilding;
	%this.EnablePainting = %this.snapshotEnablePainting;
	%this.EnableWand = %this.snapshotEnableWand;
	%this.FallingDamage = %this.snapshotFallingDamage;
	%this.PlayerDataBlock = %this.snapshotPlayerDataBlock;
	%this.PlayersUseOwnBricks = %this.snapshotPlayersUseOwnBricks;
	%this.Score_BreakBrick = %this.snapshotScore_BreakBrick;
	%this.Score_Die = %this.snapshotScore_Die;
	%this.Score_KillBot = %this.snapshotScore_KillBot;
	%this.Score_KillPlayer = %this.snapshotScore_KillPlayer;
	%this.Score_KillSelf = %this.snapshotScore_KillSelf;
	%this.Score_PlantBrick = %this.snapshotScore_PlantBrick;
	%this.RespawnTime = %this.snapshotRespawnTime;
	%this.SelfDamage = %this.snapshotSelfDamage;
	%this.StartEquip0 = %this.snapshotStartEquip0;
	%this.StartEquip1 = %this.snapshotStartEquip1;
	%this.StartEquip2 = %this.snapshotStartEquip2;
	%this.StartEquip3 = %this.snapshotStartEquip3;
	%this.StartEquip4 = %this.snapshotStartEquip4;
	%this.TimeLimit = %this.snapshotTimeLimit;
	%this.UseAllPlayersBricks = %this.snapshotUseAllPlayersBricks;
	%this.UseSpawnBricks = %this.snapshotUseSpawnBricks;
	%this.VehicleDamage = %this.snapshotVehicleDamage;
	%this.VehicleRespawnTime = %this.snapshotVehicleRespawnTime;
	%this.WeaponDamage = %this.snapshotWeaponDamage;

	// Clear snapshot settings.
	%this.snapshotBotDamage = "";
	%this.snapshotBotRespawnTime = "";
	%this.snapshotBrickDamage = "";
	%this.snapshotBrickRespawnTime = "";
	%this.snapshotEnableBuilding = "";
	%this.snapshotEnablePainting = "";
	%this.snapshotEnableWand = "";
	%this.snapshotFallingDamage = "";
	%this.snapshotPlayerDataBlock = "";
	%this.snapshotPlayersUseOwnBricks = "";
	%this.snapshotScore_BreakBrick = "";
	%this.snapshotScore_Die = "";
	%this.snapshotScore_KillBot = "";
	%this.snapshotScore_KillPlayer = "";
	%this.snapshotScore_KillSelf = "";
	%this.snapshotScore_PlantBrick = "";
	%this.snapshotRespawnTime = "";
	%this.snapshotSelfDamage = "";
	%this.snapshotStartEquip0 = "";
	%this.snapshotStartEquip1 = "";
	%this.snapshotStartEquip2 = "";
	%this.snapshotStartEquip3 = "";
	%this.snapshotStartEquip4 = "";
	%this.snapshotTimeLimit = "";
	%this.snapshotUseAllPlayersBricks = "";
	%this.snapshotUseSpawnBricks = "";
	%this.snapshotVehicleDamage = "";
	%this.snapshotVehicleRespawnTime = "";
	%this.snapshotWeaponDamage = "";

	%this.duel = "";
	%this.testDuel = "";
	%this.duelist1 = "";
	%this.duelist2 = "";
	%this.duelistName1 = "";
	%this.duelistName2 = "";
	%this.duelistRecord1 = "";
	%this.duelistRecord2 = "";
	%this.duelistScore1 = "";
	%this.duelistScore2 = "";
	%this.goal = "";
	%this.suddenDeath = "";

	%this.lastResetTime = 0;
	%this.Reset(%client);

	if (!%client.isAIControlled())
		commandToClient(%client, 'dcSetTestDuelStatus', 0);
}

function serverCmdSetMiniGameData(%client, %line)
{
	%miniGame = %client.miniGame;

	if (!isObject(%miniGame) || %miniGame.owner != %client || %miniGame.duel)
		return;

	if (%miniGame.partition)
	{
		%count = getFieldCount(%line);
		%j = -1;

		for (%i = 0; %i < %count; %i++)
		{
			%field = getField(%line, %i);
			%cmd = getWord(%field, 0);

			switch$ (%cmd)
			{
			case "EB":
			case "EP":
			case "EW":
			case "IO":
			case "T":
			default:
				%filtered = setField(%filtered, %j++, %field);
			}
		}

		%line = %filtered;
	}

	Parent::serverCmdSetMiniGameData(%client, %line);
}

}; // package Server_Dueling
