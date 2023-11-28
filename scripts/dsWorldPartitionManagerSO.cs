// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsWorldPartitionManagerSO()
{
	if (isObject(dsWorldPartitionManagerSO))
		return nameToID(dsWorldPartitionManagerSO);

	%obj = new ScriptObject(dsWorldPartitionManagerSO)
	{
		defaultMaxBricks = 10000;
	};

	%obj.freePartitions = new SimGroup();
	%obj.usedPartitions = new SimGroup();
	%obj.partitionPositionTable = new GuiTextListCtrl();

	return %obj;
}

function dsWorldPartitionManagerSO::generate(%this, %partitionSeparation, %partitionSize, %count)
{
	if (%this.freePartitions.getCount() || %this.usedPartitions.getCount())
		return;

	%this.partitionSeparation = %partitionSeparation;
	%this.partitionSize = %partitionSize;
	%partitionPositionTable = %this.partitionPositionTable;
	%partitionSizeBy2 = %partitionSize / 2;
	%turn = "0 0 0 0 0 1 " @ $piOver2;

	// Default Start
	%j = 1;
	%k = 0;
	%l = 0;
	%edge = 0;
	%pos = "0 0 0";
	%walk = "1 0 0";

	// Start at index 1
	%pos = VectorAdd(%pos, %walk);
	%pos = mFloatLength(getWord(%pos, 0), 0) SPC mFloatLength(getWord(%pos, 1), 0) SPC mFloatLength(getWord(%pos, 2), 0);
	%l = 1;
	%edge = 1;
	%walk = MatrixMulVector(%turn, %walk);

	%missionCleanup = nameToID(MissionCleanup);
	%freePartitions = %this.freePartitions;

	// Spiral generation
	// TODO: Use the Z axis
	for (%i = 1; %i <= %count; %i++)
	{
		%pos = VectorAdd(%pos, %walk);
		%pos = mFloatLength(getWord(%pos, 0), 0) SPC mFloatLength(getWord(%pos, 1), 0) SPC mFloatLength(getWord(%pos, 2), 0);
		%scaledPos = VectorScale(%pos, %partitionSeparation);

		%x = getWord(%scaledPos, 0);
		%y = getWord(%scaledPos, 1);
		%z = getWord(%scaledPos, 2);

		%obj = new ScriptObject()
		{
			class = dsWorldPartitionSO;
			position = %scaledPos;
			minX = %x - %partitionSizeBy2;
			maxX = %x + %partitionSizeBy2;
			minY = %y - %partitionSizeBy2;
			maxY = %y + %partitionSizeBy2;
			minZ = %z;
			maxZ = %z + %partitionSize;
			maxBricks = %this.defaultMaxBricks;
		};

		%freePartitions.add(%obj);
		%partitionPositionTable.addRow(%obj, %scaledPos);

		%bricks = new SimSet();
		%obj.bricks = %bricks;
		%missionCleanup.add(%bricks);

		%boundary = new SimSet();
		%obj.boundary = %boundary;
		%missionCleanup.add(%boundary);

		%k++;

		if (%k == %j)
		{
			%l++;
			%k = 0;
			%edge++;
			%walk = MatrixMulVector(%turn, %walk);

			if (%l == 2)
			{
				%l = 0;
				%j++;
			}
		}
	}
}

function dsWorldPartitionManagerSO::acquire(%this)
{
	%freePartitions = %this.freePartitions;
	%count = %freePartitions.getCount();

	if (!%count)
		return 0;

	%partition = %freePartitions.getObject(getRandom(0, %count));
	%this.usedPartitions.add(%partition);

	%partition.createBoundary();

	return %partition;
}

function dsWorldPartitionManagerSO::lookupPartition(%this, %position)
{
	%partitionSeparation = %this.partitionSeparation;
	%x = mFloatLength(mFloatLength(getWord(%position, 0) / %partitionSeparation, 0) * %partitionSeparation, 0);
	%y = mFloatLength(mFloatLength(getWord(%position, 1) / %partitionSeparation, 0) * %partitionSeparation, 0);
	%z = 0;

	%partitionPositionTable = %this.partitionPositionTable;
	%idx = %partitionPositionTable.findTextIndex(%x SPC %y SPC %z);
	if (%idx < 0)
		return 0;

	return %partitionPositionTable.getRowId(%idx);
}

function dsWorldPartitionManagerSO::release(%this, %partition)
{
	%partition.clearBricks();
	%partition.deleteBoundary();

	%this.freePartitions.add(%partition);
}

function dsWorldPartitionSO::hostBuildingSession(%this, %client)
{
	if (isObject(%this.miniGame))
		return 0;

	// TODO: Handle bricks owned by players who aren't part of the current session.

	%colorIdx = 2; // Yellow
	$MiniGameColorTaken[%colorIdx] = 0;
	%client.partition = %this;
	%miniGame = CreateMiniGameSO(%client, "Build Session", %colorIdx, 1);
	%miniGame.partition = %this;
	%miniGame.InviteOnly = 1;
	%miniGame.buildSession = 1;
	%miniGame.EnableBuilding = 1;
	%miniGame.EnablePainting = 1;
	%miniGame.EnableWand = 1;
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
	commandToClient(%client, 'dcSetBrickCount', 0, %this.maxBricks);

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
	%duelist1.partition = %this;
	%miniGame = CreateMiniGameSO(%duelist1, "Duel", %colorIdx, 1);
	%miniGame.partition = %this;
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

function dsWorldPartitionSO::addBrick(%this, %brick)
{
	%bricks = %this.bricks;
	if ((%bricks.getCount() + 1) > %this.maxBricks)
		return 0;

	%brick.partition = %partition;
	%bricks.add(%brick);

	%miniGame = %this.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && !isEventPending(%this.reportBricksEvent))
		%this.reportBricksEvent = %this.schedule(5000, reportBricks);

	return 1;
}

function dsWorldPartitionSO::reportBricks(%this)
{
	%miniGame = %this.miniGame;
	if (!isObject(%miniGame) || !%minigame.buildSession)
		return;

	%count = %this.bricks.getCount();

	if (%this.lastCount == %count)
		return;

	%miniGame.commandToAll('dcSetBrickCount', %count, %this.maxBricks);

	%this.lastCount = %count;
	%this.reportBricksEvent = %this.schedule(5000, reportBricks);
}

function dsWorldPartitionSO::removeBrick(%this, %brick)
{
	// NOTE: This function called when a brick is deleted, which will remove the brick from all SimSets that reference it, so no need to remove via script.
	// %this.bricks.remove(%brick);

	%miniGame = %this.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && !isEventPending(%this.reportBricksEvent))
		%this.reportBricksEvent = %this.schedule(5000, reportBricks);
}

function dsWorldPartitionSO::createBoundary(%this)
{
	%brickGroup = nameToID(BrickGroup_888888);
	%dataBlock = nameToID(brick64xBoundaryCubeData);
	%partitionSize = dsWorldPartitionManagerSO.partitionSize;
	%boundary = %this.boundary;
	%origin = %this.position;

	// Assume the following:
	//  - Partition size is a multiple of 32
	//  - Boundary size is 32
	//  - Partition size encapsulates the boundary

	%end = (%partitionSize - 32) / 2;
	%start = -1 * %end;
	%wallEnd = %end - 32;
	%wallStart = -1 * %wallEnd;
	%wallVertEnd = %partitionSize - 48;
	%roofZ = %partitionSize - 16;

	%instantGroupBackup = $instantGroup;
	$instantGroup = %brickGroup;

	// Generate walls

	for (%x = %wallStart; %x <= %wallEnd; %x += 32)
	for (%z = 16; %z <= %wallVertEnd; %z += 32)
	{
		%brick = new fxDTSBrick()
		{
			position = VectorAdd(%origin, %x SPC %start SPC %z);
			dataBlock = %dataBlock;
			isBoundary = 1;
			isPlanted = 1;
		};

		%boundary.add(%brick);
		%brick.setTrusted(1);
		%brick.plant();
	}

	for (%x = %wallStart; %x <= %wallEnd; %x += 32)
	for (%z = 16; %z <= %wallVertEnd; %z += 32)
	{
		%brick = new fxDTSBrick()
		{
			position = VectorAdd(%origin, %x SPC %end SPC %z);
			dataBlock = %dataBlock;
			isBoundary = 1;
			isPlanted = 1;
		};

		%boundary.add(%brick);
		%brick.setTrusted(1);
		%brick.plant();
	}

	for (%y = %wallStart; %y <= %wallEnd; %y += 32)
	for (%z = 16; %z <= %wallVertEnd; %z += 32)
	{
		%brick = new fxDTSBrick()
		{
			position = VectorAdd(%origin, %start SPC %y SPC %z);
			dataBlock = %dataBlock;
			isBoundary = 1;
			isPlanted = 1;
		};

		%boundary.add(%brick);
		%brick.setTrusted(1);
		%brick.plant();
	}

	for (%y = %wallStart; %y <= %wallEnd; %y += 32)
	for (%z = 16; %z <= %wallVertEnd; %z += 32)
	{
		%brick = new fxDTSBrick()
		{
			position = VectorAdd(%origin, %end SPC %y SPC %z);
			dataBlock = %dataBlock;
			isBoundary = 1;
			isPlanted = 1;
		};

		%boundary.add(%brick);
		%brick.setTrusted(1);
		%brick.plant();
	}

	// Generate roof

	for (%x = %start; %x <= %end; %x += 32)
	for (%y = %start; %y <= %end; %y += 32)
	{
		%brick = new fxDTSBrick()
		{
			position = VectorAdd(%origin, %x SPC %y SPC %roofZ);
			dataBlock = %dataBlock;
			isBoundary = 1;
			isPlanted = 1;
		};

		%boundary.add(%brick);
		%brick.setTrusted(1);
		%brick.plant();
	}

	$instantGroup = %instantGroupBackup;
}

function dsWorldPartitionSO::deleteBoundary(%this)
{
	%boundary = %this.boundary;

	while (%count = %boundary.getCount())
		%boundary.getObject(%count - 1).delete();
}

function dsWorldPartitionSO::ghostBoundaryToClient(%this, %client)
{
	%boundary = %this.boundary;
	%count = %boundary.getCount();

	for (%i = 0; %i < %count; %i++)
		%boundary.getObject(%i).scopeToClient(%client);
}

function dsWorldPartitionSO::testWorldBox(%this, %worldBox)
{
	%minX = getWord(%worldBox, 0);
	%minY = getWord(%worldBox, 1);
	%minZ = getWord(%worldBox, 2);
	%maxX = getWord(%worldBox, 3);
	%maxY = getWord(%worldBox, 4);
	%maxZ = getWord(%worldBox, 5);

	return %minX < %this.maxX && %minY < %this.maxY && %minZ < %this.maxZ && %maxX > %this.minX && %maxY > %this.minY && %maxZ > %this.minZ;
}

function dsWorldPartitionSO::clearBricks(%this)
{
	%bricks = %this.bricks;
	if (!%bricks.getCount())
		return 0;

	while (%count = %bricks.getCount())
		%bricks.getObject(%count - 1).delete();

	return 1;
}

function MiniGameSO::addMember(%this, %client)
{
	%client.resetGhosting();

	Parent::addMember(%this, %client);

	%camera = %client.camera;
	%player = %client.player;

	%client.suppressGhostAlwaysObjectsReceived = 1;
	%client.activateGhosting();

	if (isObject(%camera))
		%camera.scopeToClient(%client);
	if (isObject(%player))
		%player.scopeToClient(%client);

	%partition = %this.partition;
	if (%partition)
		%client.partition = %partition;

	// TODO: Verify whether this is necessary
	// %partition = %this.partition ? %this.partition : %client.partition;
	// if (%partition)
	// 	%partition.ghostBoundaryToClient(%client);

	if (%client.miniGame != %this)
		return;

	// Will always be false for the owner when using CreateMinigameSO.
	if (%this.buildSession)
	{
		%this.commandToAll('dcSetBuildSessionMember', %client, %client.name);

		if (!%client.isAIControlled())
		{
			commandToClient(%client, 'dcSetBrickCount', %this.partition.bricks.getCount(), %this.partition.maxBricks);
			commandToClient(%client, 'dcSetBuilding', 1);
		}
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

	%numMembers = %this.numMembers;
	for (%i = 0; %i < %numMembers; %i++)
	{
		%member = %this.member[%i];
		%member.resetGhosting();
		%member[%i] = %member;
	}

	Parent::endGame(%this);

	%this.owner.InstantRespawn();

	for (%i = 0; %i < %numMembers; %i++)
	{
		%member = %member[%i];
		%camera = %member.camera;
		%player = %member.player;

		%member.suppressGhostAlwaysObjectsReceived = 1;
		%member.activateGhosting();
		%member.partition = "";

		if (isObject(%camera))
			%camera.scopeToClient(%member);
		if (isObject(%player))
			%player.scopeToClient(%member);
	}

	%partition = %this.partition;
	if (isObject(%partition))
	{
		%partition.miniGame = "";
		dsWorldPartitionManagerSO.release(%partition);
		%partition = "";
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

	%client.resetGhosting();

	Parent::removeMember(%this, %client);

	%client.partition = "";

	%camera = %client.camera;
	%player = %client.player;

	%client.suppressGhostAlwaysObjectsReceived = 1;
	%client.activateGhosting();

	if (isObject(%camera))
		%camera.scopeToClient(%client);
	if (isObject(%player))
		%player.scopeToClient(%client);
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

function GameConnection::getSpawnPoint(%this)
{
	// TODO: If no minigame, pick spawnpoint from "Lobby" partition

	return Parent::getSpawnPoint(%this);
}

function MiniGameSO::pickSpawnPoint(%this, %client)
{
	if (%this.partition)
	{
		// TODO: Select from spawn bricks in partition or random location in partition.
		return %this.partition.position;
	}

	return Parent::pickSpawnPoint(%this, %client);
}

function GameConnection::setLoadingIndicator(%this, %a)
{
	if (%a)
		return; // Disable
	else if (!%this.isAIControlled())
		commandToClient(%this, 'setLoadingIndicator', %a);
}

function GameConnection::onGhostAlwaysObjectsReceived(%this)
{
	if (%this.suppressGhostAlwaysObjectsReceived)
	{
		%this.suppressGhostAlwaysObjectsReceived = "";
		return;
	}

	Parent::onGhostAlwaysObjectsReceived(%this);
}

function serverCmdDropCameraAtPlayer(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession)
	{
		%backupAdmin = %client.isAdmin;
		%client.isAdmin = 1;
		%override = 1;
	}

	Parent::serverCmdDropCameraAtPlayer(%client);

	if (%override)
		%client.isAdmin = %backupAdmin;
}

function serverCmdDropPlayerAtCamera(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession)
	{
		%backupAdmin = %client.isAdmin;
		%client.isAdmin = 1;
		%override = 1;
	}

	Parent::serverCmdDropPlayerAtCamera(%client);

	if (%override)
		%client.isAdmin = %backupAdmin;
}

function serverCmdWarp(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession)
	{
		%backupAdmin = %client.isAdmin;
		%client.isAdmin = 1;
		%override = 1;
	}

	Parent::serverCmdWarp(%client);

	if (%override)
		%client.isAdmin = %backupAdmin;
}

function serverCmdSpy(%client, %target)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession)
	{
		if (!isObject(%target))
			%target = findClientByName(%target);

		if (isObject(%target) && %target.miniGame == %miniGame)
		{
			%backupAdmin = %client.isAdmin;
			%client.isAdmin = 1;
			%override = 1;
		}
	}

	Parent::serverCmdSpy(%client, %target);

	if (%override)
		%client.isAdmin = %backupAdmin;
}

function fxDTSBrick::plant(%this)
{
	%result = Parent::plant(%this);

	if (%result == 2)
		%result = 0;

	if (!%result)
	{
		%group = %this.getGroup();
		if (!%this.isBoundary && (!isObject(%group) || %group != nameToID(BrickGroup_888888)) && (%partition = dsWorldPartitionManagerSO.lookupPartition(%this.position)))
		{
			%client = %this.client;
			if ((isObject(%client) && %client.partition != %partition) || !%partition.testWorldBox(%this.getWorldBox()))
				return 3;

			if (!%partition.addBrick(%this))
				return 3;
		}

		%this.isBaseplate = 1;
		%this.willCauseChainKill();
	}

	return %result;
}

function fxDTSBrick::onRemove(%this)
{
	%partition = %this.partition;
	if (isObject(%partition))
		%partition.removeBrick(%this);

	Parent::onRemove(%this);
}

}; // package Server_Dueling
