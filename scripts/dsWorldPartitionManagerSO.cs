// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsWorldPartitionManagerSO()
{
	if (isObject(dsWorldPartitionManagerSO))
		return nameToID(dsWorldPartitionManagerSO);

	%obj = new ScriptObject(dsWorldPartitionManagerSO)
	{
		defaultMaxBricks = 20000;
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

	%missionCleanup = nameToID(MissionCleanup);
	%freePartitions = %this.freePartitions;

	// Spiral generation
	// TODO: Use the Z axis
	for (%i = 0; %i < %count; %i++)
	{
		%scaledPos = VectorScale(%pos, %partitionSeparation);
		%pos = VectorAdd(%pos, %walk);
		%pos = mFloatLength(getWord(%pos, 0) + 1e-9, 0) SPC mFloatLength(getWord(%pos, 1) + 1e-9, 0) SPC mFloatLength(getWord(%pos, 2) + 1e-9, 0);

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

		%spawnPoints = new SimSet();
		%obj.spawnPoints = %spawnPoints;
		%missionCleanup.add(%spawnPoints);

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

	// Always keep the center partition's boundary loaded
	%centerPartition = %freePartitions.getObject(0);
	%this.centerPartition = %centerPartition;
	%centerPartition.createBoundary();
	%missionCleanup.add(%centerPartition);
}

function dsWorldPartitionManagerSO::setMaxBricks(%this, %maxBricks)
{
	%group = %this.freePartitions;
	%count = %group.getCount();
	for (%i = 0; %i < %count; %i++)
		%group.getObject(%i).maxBricks = %maxBricks;

	%group = %this.usedPartitions;
	%count = %group.getCount();
	for (%i = 0; %i < %count; %i++)
		%group.getObject(%i).maxBricks = %maxBricks;

	%this.centerPartition.maxBricks = %maxBricks;
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
	%x = mFloatLength(mFloatLength(getWord(%position, 0) / %partitionSeparation, 0) * %partitionSeparation + 1e-9, 0);
	%y = mFloatLength(mFloatLength(getWord(%position, 1) / %partitionSeparation, 0) * %partitionSeparation + 1e-9, 0);
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
	%miniGame.BrickDamage = 0;
	%miniGame.InviteOnly = 0;
	%miniGame.buildSession = 1;
	%miniGame.EnableBuilding = 1;
	%miniGame.EnablePainting = 1;
	%miniGame.EnableWand = 1;
	%miniGame.FallingDamage = 0;
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

	dsChallengeManagerSO.broadcastPlayerUpdate(%client, 2, %client);

	%miniGame.Reset(%client);

	%path = dsMapManagerSO.directory @ %client.bl_id @ "/.backup.cs";
	if (isFile(%path))
	{
		// NOTE: Backups are auto-loaded for now, so people without the client can build without losing progress.
		%miniGame.partition.loadBricks(%path);
		// commandToClient(%client, 'dcLoadBackup');
	}

	return 1;
}

function dsWorldPartitionSO::hostDuelingSession(%this, %duelist1, %duelist2, %weapon, %goal, %practice, %map)
{
	if (isObject(%this.miniGame))
		return 0;

	// TODO: Handle bricks owned by players who aren't part of the current session.

	%ranked = !%practice;

	%colorIdx = 0; // Red
	$MiniGameColorTaken[%colorIdx] = 0;
	%duelist1.partition = %this;
	%miniGame = CreateMiniGameSO(%duelist1, %practice ? "Practice Duel" : "Ranked Duel", %colorIdx, 1);
	%miniGame.duel = 1;
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

	dsChallengeManagerSO.broadcastPlayerUpdate(%duelist1, 4, %duelist1);
	dsChallengeManagerSO.broadcastPlayerUpdate(%duelist2, 4, %duelist2);

	%miniGame.map = %map;
	%miniGame.target = %weapon;

	if (%map)
	{
		%name = %map.name;
		%directory = dsMapManagerSO.directory @ %map.submitterID @ "/";
		%path = %directory @ %name @ "-bricks.cs";
		%this.loadBricks(%path);
		%miniGame.messageAll('', "\c3Map \c3\"" @ %name @ "\"\c2!");
		%miniGame.messageAll('', "\c4Builders: \c3" @ %map.getOwnersPrettyString());
	}

	%miniGame.duelReady = 1;
	%minigame.duelEnded = "";
	%miniGame.Reset(%duelist1);

	if (!%duelist1.isAIControlled())
		commandToClient(%duelist1, 'dcCloseWindow');
	if (!%duelist2.isAIControlled())
		commandToClient(%duelist2, 'dcCloseWindow');

	%miniGame.messageAll('', "\c2[FT" @ %miniGame.goal @ ",WB2] \c3" @ %miniGame.duelistName1 @ " \c2[" @ %miniGame.duelistScore1 @ " - " @ %miniGame.duelistScore2 @ "] \c3" @ %miniGame.duelistName2);

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

	%brick.partition = %this;
	%bricks.add(%brick);

	if (nameToID(%brick.getDataBlock()) == nameToID(brickSpawnPointData))
		%this.spawnPoints.add(%brick);

	%miniGame = %this.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession)
	{
		if (%bricks.getCount() == 1)
			%this.reportBricks();
		else if (!isEventPending(%this.reportBricksEvent))
			%this.reportBricksEvent = %this.schedule(5000, reportBricks);
	}

	return 1;
}

function dsWorldPartitionSO::reportBricks(%this)
{
	%miniGame = %this.miniGame;
	if (!isObject(%miniGame) || !%miniGame.buildSession)
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
	// NOTE: This function is called when a brick is deleted, which will remove the brick from all SimSets that reference it, so no need to remove via script.
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

function dsWorldPartitionSO::loadBricks(%this, %path, %offset)
{
	%instantGroupBackup = $instantGroup;
	$instantGroup = nameToID(BrickGroup_999999);

	if ($Server::Dedicated)
	{
		// Hide loading lines from console window. They will still appear in the log.
		enableWinConsole(0);
		%result = exec(%path);
		%promptBackup = $Con::Prompt;
		$Con::Prompt = "";
		enableWinConsole(1);
		$Con::Prompt = %promptBackup;
	}
	else
	{
		%result = exec(%path);
	}

	if (!%result)
	{
		error("Could not load bricks.");
		return;
	}

	$instantGroup = %instantGroupBackup;

	%bricks = nameToID(SavedBricks);
	MissionCleanup.add(%bricks);

	%count = %bricks.getCount();
	%origin = VectorAdd(%this.position, %offset);
	%planted = 0;
	%lastLoadedBrickBackup = $LastLoadedBrick;
	%lastServer_LoadFileObj = $Server_LoadFileObj;

	for (%i = 0; %i < %count; %i++)
	{
		%brick = %bricks.getObject(%i);
		%bl_id = %brick.stackBL_ID;
		%brickGroup = "BrickGroup_" @ %bl_id;

		if (!isObject(%brickGroup))
		{
			%brickGroup = new SimGroup(%brickGroup);
			%brickGroup.bl_id = %bl_id;
			%brickGroup.name = "\c1BL_ID: " @ %bl_id @ "\c1\c0";
			%brickGroup.client = 0;
			mainBrickGroup.add(%brickGroup);
		}

		%brickGroup.add(%brick);
		%brick.client = %brickGroup.client;
		%brick.setTransform(VectorAdd(%brick.position, %origin) SPC getWords(%brick.getTransform(), 3, 6));
		%brick.setTrusted(1);

		$Server_LoadFileObj = %brick;
		$LastLoadedBrick = %brick;
		if (%brick.plant())
		{
			%brick.schedule(0, delete);
			continue;
		}

		%planted++;
		%brick.setColliding(%brick.isColliding);
		%brick.setRayCasting(%brick.isRayCasting);
		%brick.setRendering(%brick.isRendering);
		%brick.isColliding = "";
		%brick.isRayCasting = "";
		%brick.isRendering = "";

		if ((%textureLookup = %brick.textureLookup) !$= "")
		{
			%brick.printID = 0;
			%brick.setPrint($printNameTable[%textureLookup]);
			%brick.textureLookup = "";
		}

		if ((%emitter = %brick.emitterDataBlock) !$= "")
		{
			%brick.setEmitter(%emitter);
			%brick.emitterDataBlock = "";
		}

		if ((%item = %brick.itemDataBlock) !$= "")
		{
			%brick.setItem(%item);
			%brick.itemDataBlock = "";
		}

		if ((%light = %brick.lightDataBlock) !$= "")
		{
			%brick.setLight(%light);
			%brick.lightDataBlock = "";
		}

		%numEvents = %brick.numEvents;
		for (%j = 0; %j < %numEvents; %j++)
		{
			%targetIdx = %brick.eventTargetIdx[%j];
			%targetClass = %targetIdx == -1 ? "fxDTSBrick" : getWord($InputEvent_TargetList["fxDTSBrick", %brick.eventInputIdx[%j]], %targetIdx * 2 + 1);

			for (%k = 0; %k < 4; %k++)
			{
				%field = getField($OutputEvent_parameterList[%targetClass, %brick.eventOutputIdx[%k]], %j);
				%dataType = getWord(%field, 0);

				if (%dataType $= "dataBlock" && isObject(%db = %brick.eventOutputParameter[%j, %k + 1]))
					%brick.eventOutputParameter[%j, %k + 1] = nameToID(%db);
			}
		}
	}

	%bricks.delete();
	$LastLoadedBrick = %lastLoadedBrickBackup;
	$Server_LoadFileObj = %lastServer_LoadFileObj;

	return %planted;
}

function ServerLoadSaveFile_End()
{
	$Server_LoadFileObj.close();
	Parent::ServerLoadSaveFile_End();
}

function dsWorldPartitionSO::saveBricks(%this, %path, %saveOwners, %saveWorldBox)
{
	%bricks = %this.bricks;
	%count = %bricks.getCount();

	if (!%count)
		return 0;

	%bricks.setName("SavedBricks");
	%origin = %this.position;

	%emitters = new GuiTextListCtrl();
	%items = new GuiTextListCtrl();
	%lights = new GuiTextListCtrl();

	if (%saveOwners)
		%owners = new GuiTextListCtrl();

	%saved = 0;

	for (%i = 0; %i < %count; %i++)
	{
		%brick = %bricks.getObject(%i);
		if (%brick.isDead())
			continue;

		%brickGroup = %brick.getGroup();
		%bl_id = %brickGroup.bl_id;
		%brick.client = 0;
		%brick.color = getColorIDTable(%brick.colorID); // TODO: Use table defined for partition
		%brick.partition = "";
		%brick.position = VectorSub(%brick.position, %origin);
		%brick.isColliding = %brick.isColliding();
		%brick.isRayCasting = %brick.isRayCasting();
		%brick.isRendering = %brick.isRendering();
		%brick.stackBL_ID = %bl_id;

		if (%saveOwners && %owners.getRowNumById(%bl_id) < 0)
			%owners.addRow(%bl_id, StripMLControlChars(%brickGroup.name));

		if (isObject(%emitter = %brick.emitter))
		{
			%brick.emitterDataBlock = %emitter.getEmitterDataBlock().getName();
			%emitters.addRow(%brick, %emitter);
		}

		if (isObject(%item = %brick.item))
		{
			%brick.itemDataBlock = %item.getDataBlock().getName();
			%items.addRow(%brick, %item);
		}

		if (isObject(%light = %brick.light))
		{
			%brick.lightDataBlock = %light.getDataBlock().getName();
			%lights.addRow(%brick, %light);
		}

		// This is needed to make doors openable after load. It's OK if the door toggle threshold is bypassed on save.
		%brick.lastDoorDataBlockSwitch = "";

		%brick.emitter = "";
		%brick.item = "";
		%brick.light = "";

		if (%brick.printID)
		{
			%texture = getPrintTexture(%brick.printID);

			%start = 14; // strlen("Add-Ons/Print_")
			%end = strpos(%texture, "_", %start + 1);
			%aspectRatio = getSubStr(%texture, %start, %end - %start);

			%brick.textureLookup = %aspectRatio @ "/" @ fileBase(%texture);
		}

		%numEvents = %brick.numEvents;
		for (%j = 0; %j < %numEvents; %j++)
		{
			%targetIdx = %brick.eventTargetIdx[%j];
			%targetClass = %targetIdx == -1 ? "fxDTSBrick" : getWord($InputEvent_TargetList["fxDTSBrick", %brick.eventInputIdx[%j]], %targetIdx * 2 + 1);

			for (%k = 0; %k < 4; %k++)
			{
				%field = getField($OutputEvent_parameterList[%targetClass, %brick.eventOutputIdx[%k]], %j);
				%dataType = getWord(%field, 0);

				if (%dataType $= "dataBlock" && isObject(%db = %brick.eventOutputParameter[%j, %k + 1]))
					%brick.eventOutputParameter[%j, %k + 1] = %db.getName();
			}
		}

		%saved++;
	}

	if (%saved)
	{
		if (%saveWorldBox)
		{
			%minX = inf;
			%minY = inf;
			%minZ = inf;
			%maxX = -inf;
			%maxY = -inf;
			%maxZ = -inf;

			for (%i = 0; %i < %count; %i++)
			{
				%brick = %bricks.getObject(%i);
				%worldBox = %brick.getWorldBox();
				%worldBox = VectorSub(getWords(%worldBox, 0, 2), %origin) SPC VectorSub(getWords(%worldBox, 3, 5), %origin);

				%brickMinX = getWord(%worldBox, 0);
				%brickMinY = getWord(%worldBox, 1);
				%brickMinZ = getWord(%worldBox, 2);
				%brickMaxX = getWord(%worldBox, 3);
				%brickMaxY = getWord(%worldBox, 4);
				%brickMaxZ = getWord(%worldBox, 5);

				%minX = %minX < %brickMinX ? %minX : %brickMinX;
				%minY = %minY < %brickMinY ? %minY : %brickMinY;
				%minZ = %minZ < %brickMinZ ? %minZ : %brickMinZ;
				%maxX = %maxX > %brickMaxX ? %maxX : %brickMaxX;
				%maxY = %maxY > %brickMaxY ? %maxY : %brickMaxY;
				%maxZ = %maxZ > %brickMaxZ ? %maxZ : %brickMaxZ;
			}

			%this.saveWorldBox = %minX SPC %minY SPC %minZ SPC %maxX SPC %maxY SPC %maxZ;
		}

		%bricks.save(%path);

		for (%i = 0; %i < %count; %i++)
		{
			%brick = %bricks.getObject(%i);
			%brick.partition = %this;

			%brick.position = VectorAdd(%brick.position, %origin);
			%brick.client = %brick.getGroup().client;

			if ((%search = %emitters.getRowNumById(%brick)) != -1)
				%brick.emitter = %emitters.getRowText(%search);

			if ((%search = %items.getRowNumById(%brick)) != -1)
				%brick.item = %items.getRowText(%search);

			if ((%search = %lights.getRowNumById(%brick)) != -1)
				%brick.light = %lights.getRowText(%search);

			%numEvents = %brick.numEvents;
			for (%j = 0; %j < %numEvents; %j++)
			{
				%targetIdx = %brick.eventTargetIdx[%j];
				%targetClass = %targetIdx == -1 ? "fxDTSBrick" : getWord($InputEvent_TargetList["fxDTSBrick", %brick.eventInputIdx[%j]], %targetIdx * 2 + 1);

				for (%k = 0; %k < 4; %k++)
				{
					%field = getField($OutputEvent_parameterList[%targetClass, %brick.eventOutputIdx[%k]], %j);
					%dataType = getWord(%field, 0);

					if (%dataType $= "dataBlock" && isObject(%db = %brick.eventOutputParameter[%j, %k + 1]))
						%brick.eventOutputParameter[%j, %k + 1] = nameToID(%db);
				}
			}
		}

		%bricks.setName("");

		if (%saveOwners)
		{
			%owners.sort(0, 1);
			%ownerCount = %owners.rowCount(); // Should always be at least 1

			%ownerString = %owners.getRowId(0) SPC %owners.getRowText(0);

			for (%i = 1; %i < %ownerCount; %i++)
				%ownerString = %ownerString TAB %owners.getRowId(%i) SPC %owners.getRowText(%i);

			%this.saveOwners = %ownerString;

		}
	}

	if (%saveOwners)
		%owners.delete();

	%emitters.delete();
	%items.delete();
	%lights.delete();

	return %saved;
}

function dsWorldPartitionSO::worldBoxCollide(%this, %worldBox)
{
	%minX = getWord(%worldBox, 0);
	%minY = getWord(%worldBox, 1);
	%minZ = getWord(%worldBox, 2);
	%maxX = getWord(%worldBox, 3);
	%maxY = getWord(%worldBox, 4);
	%maxZ = getWord(%worldBox, 5);

	return %minX < %this.maxX && %minY < %this.maxY && %minZ < %this.maxZ && %maxX > %this.minX && %maxY > %this.minY && %maxZ > %this.minZ;
}

function dsWorldPartitionSO::worldBoxInside(%this, %worldBox)
{
	%minX = getWord(%worldBox, 0);
	%minY = getWord(%worldBox, 1);
	%minZ = getWord(%worldBox, 2);
	%maxX = getWord(%worldBox, 3);
	%maxY = getWord(%worldBox, 4);
	%maxZ = getWord(%worldBox, 5);

	return %maxX <= %this.maxX && %maxY <= %this.maxY && %maxZ <= %this.maxZ && %minX >= %this.minX && %minY >= %this.minY && %minZ >= %this.minZ;
}

function dsWorldPartitionSO::centerBricks(%this)
{
	%bricks = %this.bricks;
	if (!%bricks.getCount())
		return 0;

	%path = dsMapManagerSO.directory @ ".temp-centerBricks.cs";
	if (!%this.saveBricks(%path, 0, 1))
		return 0;

	%center = getBoxCenter(%this.saveWorldBox);
	%offset = setWord(VectorScale(%center, -1), 2, 0);

	// Offset must be a multiple of 0.5
	%offset = VectorScale(%offset, 2);
	%offset = (getWord(%offset, 0) | 0) SPC (getWord(%offset, 1) | 0) SPC (getWord(%offset, 2) | 0);
	%offset = VectorScale(%offset, 0.5);

	if (VectorLen(%offset) < 1 || !%this.clearBricks() || !%this.loadBricks(%path, %offset))
		return 0;

	return 1;
}

function dsWorldPartitionSO::clearBricks(%this)
{
	%bricks = %this.bricks;
	if (!%bricks.getCount())
		return 0;

	while (%count = %bricks.getCount())
		%bricks.getObject(%count - 1).delete();

	%this.reportBricks();

	return 1;
}

function dsWorldPartitionSO::pickSpawnPoint(%this)
{
	%spawnPoints = %this.spawnPoints;
	%count = %spawnPoints.getCount();
	%lastSpawnPoint = %this.lastSpawnPoint;
	%spawnPoint = %lastSpawnPoint;

	if (%count)
	{
		%spawnPoint = %spawnPoints.getObject(getRandom(0, %count - 1));
		%spawnPoints.remove(%spawnPoint);

		if (isObject(%lastSpawnPoint))
			%spawnPoints.add(%lastSpawnPoint);

		%this.lastSpawnPoint = %spawnPoint;
	}

	if (isObject(%spawnPoint))
		return VectorSub(%spawnPoint.position, "0 0 1.3") SPC getWords(%spawnPoint.getTransform(), 3);

	return VectorAdd(pickSpawnPoint(), %this.position);
}

function MiniGameSO::addMember(%this, %client)
{
	%client.resetGhosting();

	Parent::addMember(%this, %client);

	%camera = %client.camera;
	%player = %client.player;

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
		%owner = %this.owner;
		if (isObject(%owner) && !%owner.isAIControlled())
			commandToClient(%owner, 'dcSetBuildSessionMember', %client, %client.name);

		if (!%client.isAIControlled())
		{
			commandToClient(%client, 'dcSetBrickCount', %this.partition.bricks.getCount(), %this.partition.maxBricks);
			commandToClient(%client, 'dcSetBuilding', 1);
		}
	}
	else if (%this.duel)
	{
		if (%this.duelist1 != %client && %this.duelist2 != %client)
		{
			// Trigger spectator mode on player spawn attempt.
			dsChallengeManagerSO.broadcastPlayerUpdate(%client, 5, %client);
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
	if (%this.duelEnded)
		return;

	switch (%this.duelResult)
	{
	case 1:
		%this.duelistScore1 += 1;

		if (%this.ranked)
			dsStatManagerSO.addRound(%this.StartEquip0, %this.duelistRecord1, %this.duelistRecord2);
	case 2:
		%this.duelistScore2 += 1;

		if (%this.ranked)
			dsStatManagerSO.addRound(%this.StartEquip0, %this.duelistRecord2, %this.duelistRecord1);
	case 3:
		%this.duelistScore1 += 1;
		%this.duelistScore2 += 1;

		if (%this.ranked)
			dsStatManagerSO.addRound(%this.StartEquip0, %this.duelistRecord1, %this.duelistRecord2, 1);
	}

	%duelist1 = %this.duelist1;
	%duelist2 = %this.duelist2;
	%end = 0;
	%ranked = %this.ranked;

	if (%this.duelResult != 3 || isObject(%leaving))
	{
		%sdn1 = %this.suddenDeath && %this.duelResult == 1;
		%sdn2 = %this.suddenDeath && %this.duelResult == 2;
		%tko1 = %duelist2 == %leaving;
		%tko2 = %duelist1 == %leaving;
		%wb21 = !%this.suddenDeath && %this.duelistScore1 >= %this.goal && (%this.duelistScore1 - %this.duelistScore2) >= 2;
		%wb22 = !%this.suddenDeath && %this.duelistScore2 >= %this.goal && (%this.duelistScore2 - %this.duelistScore1) >= 2;

		if (%sdn1 || %tko1 || %wb21)
		{
			if (%ranked)
				dsStatManagerSO.addDuel(%this.StartEquip0, %this.duelistRecord1, %this.duelistRecord2);
			%result = %this.duelistName1;
			%end = 1;
		}
		else if (%sdn2 || %tko2 || %wb22)
		{
			if (%ranked)
				dsStatManagerSO.addDuel(%this.StartEquip0, %this.duelistRecord2, %this.duelistRecord1);

			%result = %this.duelistName2;
			%end = 1;
		}
	}

	if (%end)
	{
		if (%leaving)
			%state = "[TKO]";
		else if (%this.suddenDeath)
			%state = "[SDN]";
		else
			%state = "[FT" @ %this.goal @ ",WB2]";

		%secret = getRandom(0, 99) ? "" : " \c0G\c3R\c2A\c4T\c1Z\c5!";
		%this.messageAll('', "\c3" @ %result @ " \c5won the duel!" @ %secret);
		%this.messageAll('', "\c5" @ %state @ " \c3" @ %this.duelistName1 @ " \c5[" @ %this.duelistScore1 @ " - " @ %this.duelistScore2 @ "] \c3" @ %this.duelistName2);
	}
	else
	{
		if (%leaving)
			%state = "[TKO]";
		else if (%this.suddenDeath)
			%state = "[SDN]";
		else
			%state = "[FT" @ %this.goal @ ",WB2]";

		switch (%this.duelResult)
		{
		case 1:
			%result = "\c3" @ %this.duelistName1 @ " \c2won the round!";
		case 2:
			%result = "\c3" @ %this.duelistName2 @ " \c2won the round!";
		case 3:
			%result = "\c2Draw Round!";
		}

		%this.messageAll('', %result);
		%this.messageAll('', "\c2" @ %state @ " \c3" @ %this.duelistName1 @ " \c2[" @ %this.duelistScore1 @ " - " @ %this.duelistScore2 @ "] \c3" @ %this.duelistName2);
	}

	if (!%end && !%this.suddenDeath && (%this.duelistScore1 == dsChallengeManagerSO.suddenDeathScore || %this.duelistScore2 == dsChallengeManagerSO.suddenDeathScore))
	{
		%this.messageAll('', "\c0Sudden Death started! Duel is decided by next kill.");
		%this.suddenDeath = 1;
	}
	else if (%end)
	{
		%this.duelEnded = 1;

		if (!%this.testDuel)
		{
			%map = %this.map;
			%target = %this.target;

			%map.duels[%this.target.getName()]++;

			if (isObject(%duelist1) && !%duelist1.isAIControlled())
			{
				%duelist1.lastMap = %map;
				%duelist1.lastTarget = %target;
				commandToClient(%duelist1, 'dcMapRating');

				if (%ranked)
					commandToClient(%duelist1, 'dcRefreshStats');
			}

			if (isObject(%duelist2) && !%duelist2.isAIControlled())
			{
				%duelist2.lastMap = %map;
				%duelist2.lastTarget = %target;
				commandToClient(%duelist2, 'dcMapRating');

				if (%ranked)
					commandToClient(%duelist2, 'dcRefreshStats');
			}
		}

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
		if (%this.duelReady && %this.checkResults())
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
		%this.BottomPrintAll("\c2Draw!", 5, 1);
	}
	else if (%player2Dead)
	{
		%this.duelResult = 1;
		%this.BottomPrintAll("\c3" @ %this.duelistName1 @ " \c2won!", 5, 1);
	}
	else if (%player1Dead)
	{
		%this.duelResult = 2;
		%this.BottomPrintAll("\c3" @ %this.duelistName2 @ " \c2won!", 5, 1);
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
		%path = dsMapManagerSO.directory @ %this.owner.bl_id @ "/.backup.cs";

		if (%this.partition.bricks.getCount())
			%this.partition.saveBricks(%path);
		else if (isFile(%path))
			fileDelete(%path);
	}
	else if (%this.duel)
	{
		%duelist1 = %this.duelist1;
		%duelist2 = %this.duelist2;

		if (isObject(%duelist1) && !%duelist1.isAIControlled())
			commandToClient(%duelist1, 'dcSetDueling', 0);

		if (isObject(%duelist2) && !%duelist2.isAIControlled())
			commandToClient(%duelist2, 'dcSetDueling', 0);
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

		%member.activateGhosting();
		%member.partition = dsWorldPartitionManagerSO.centerPartition;
		dsChallengeManagerSO.broadcastPlayerUpdate(%member, %member.dcClient ? 1 : 0, %member);

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
	if (%this.buildSession && %this.owner != %client)
	{
		if (!%client.isAIControlled())
			commandToClient(%client, 'dcSetBuilding', 0);

		%this.commandToAllExcept(%client, 'dcRemoveBuildSessionMember', %client);
	}

	if (%this.duel && (%this.duelist1 == %client || %this.duelist2 == %client))
	{
		if (!%this.buildSession && !%client.isAIControlled())
			commandToClient(%client, 'dcSetDueling', 0);

		if (%this.checkResults(%client))
			%this.callResults(%client);

		if (%this.owner != %client)
			%this.stopDuel(%this.owner);
	}

	// By resetting the ghosting for the owner twice (another time in endGame),
	// the owner gets in a bad state.
	if (%client != %this.owner)
		%client.resetGhosting();

	Parent::removeMember(%this, %client);

	%client.partition = dsWorldPartitionManagerSO.centerPartition;

	if (%client != %this.owner)
	{
		%camera = %client.camera;
		%player = %client.player;

		%client.activateGhosting();

		if (isObject(%camera))
			%camera.scopeToClient(%client);
		if (isObject(%player))
			%player.scopeToClient(%client);
	}
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

	%partition = %this.partition;
	if (%partition && isObject(%partition.lastSpawnPoint))
	{
		%partition.spawnPoints.add(%partition.lastSpawnPoint);
		%partition.lastSpawnPoint = "";
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
	%this.updateEnableBuilding();
	%this.updateEnablePainting();
	%this.duelReady = 1;
	%this.duelEnded = "";
	%this.Reset(%client);

	if (!%duelist1.isAIControlled())
		commandToClient(%duelist1, 'dcCloseWindow');
	if (!%duelist2.isAIControlled())
		commandToClient(%duelist2, 'dcCloseWindow');
	if (!%client.isAIControlled())
		commandToClient(%client, 'dcSetTestDuelStatus', 1);

	%this.messageAll('', "\c2[FT" @ %this.goal @ ",WB2] \c3" @ %this.duelistName1 @ " \c2[" @ %this.duelistScore1 @ " - " @ %this.duelistScore2 @ "] \c3" @ %this.duelistName2);
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
	%this.duelReady = "";
	%this.duelEnded = 1;
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
	%this.updateEnableBuilding();
	%this.updateEnablePainting();
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
			case "BD":
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
	if (!isObject(%this.miniGame))
		return dsWorldPartitionManagerSO.centerPartition.pickSpawnPoint();

	return Parent::getSpawnPoint(%this);
}

function MiniGameSO::pickSpawnPoint(%this, %client)
{
	if (%partition = %this.partition)
		return %partition.pickSpawnPoint();

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
	if (%this.hasSpawnedOnce)
	{
		// This is necessary to prevent resetting the client state after a call to GameConnection::activateGhosting.
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

function serverCmdMagicWand(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%minigame.testDuel)
	{
		%backupAdmin = %client.isAdmin;
		%client.isAdmin = 1;
		%override = 1;
	}

	Parent::serverCmdMagicWand(%client);

	if (%override)
		%client.isAdmin = %backupAdmin;
}

function AdminWandImage::onHitObject(%this, %player, %a, %b, %c, %d)
{
	%client = %player.client;
	if (isObject(%client))
	{
		%miniGame = %client.miniGame;
		if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client)
		{
			%backupAdmin = %client.isAdmin;
			%client.isAdmin = 1;
			%override = 1;
		}
	}

	Parent::onHitObject(%this, %player, %a, %b, %c, %d);

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

			if ((%this != $LastLoadedBrick && isObject(%client) && isObject(%client.partition) && %client.partition != %partition) || !%partition.worldBoxInside(%this.getWorldBox()))
			{
				%partition.plantErrorTooFar = 1;
				return 6;
			}

			if (!%partition.addBrick(%this))
				return 6;
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

function SimGroup::addSpawnBrick(%this, %brick)
{
	// Disabled
}

function SimGroup::removeSpawnBrick(%this, %brick)
{
	// Disabled
}

function startRayracer()
{
	// Disabled
}

function dsClearCenter()
{
	dsWorldPartitionManagerSO.centerPartition.clearBricks();
}

function dsSaveCenter()
{
	%path = dsMapManagerSO.directory @ ".center.cs";
	dsWorldPartitionManagerSO.centerPartition.saveBricks(%path, 0, 0);
}

function dsLoadCenter()
{
	%path = dsMapManagerSO.directory @ ".center.cs";

	if (isFile(%path))
		dsWorldPartitionManagerSO.centerPartition.loadBricks(%path);
}

function MiniGameSO::updateEnableBuilding(%this)
{
	Parent::updateEnableBuilding(%this);

	if (%this.buildSession && !%this.miniGame.testDuel)
		commandToClient(%this.owner, 'SetBuildingDisabled', 0);
}

function MiniGameSO::updateEnablePainting(%this)
{
	Parent::updateEnablePainting(%this);

	if (%this.buildSession && !%this.miniGame.testDuel)
		commandToClient(%this.owner, 'SetPaintingDisabled', 0);
}

function serverCmdBuyBrick(%client, %a, %b)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnableBuilding = %miniGame.EnableBuilding;
		%miniGame.EnableBuilding = 1;
		%override = 1;
	}

	Parent::serverCmdBuyBrick(%client, %a, %b);

	if (%override)
		%miniGame.EnableBuilding = %backupEnableBuilding;
}

function serverCmdInstantUseBrick(%client, %a)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnableBuilding = %miniGame.EnableBuilding;
		%miniGame.EnableBuilding = 1;
		%override = 1;
	}

	Parent::serverCmdInstantUseBrick(%client, %a);

	if (%override)
		%miniGame.EnableBuilding = %backupEnableBuilding;
}

function serverCmdPlantBrick(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnableBuilding = %miniGame.EnableBuilding;
		%miniGame.EnableBuilding = 1;
		%override = 1;
	}

	Parent::serverCmdPlantBrick(%client);

	if (%override)
		%miniGame.EnableBuilding = %backupEnableBuilding;
}

function serverCmdUseFXCan(%client, %a)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnablePainting = %miniGame.EnablePainting;
		%miniGame.EnablePainting = 1;
		%override = 1;
	}

	Parent::serverCmdUseFXCan(%client, %a);

	if (%override)
		%miniGame.EnablePainting = %backupEnablePainting;
}

function serverCmdUseInventory(%client, %a)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnableBuilding = %miniGame.EnableBuilding;
		%miniGame.EnableBuilding = 1;
		%override = 1;
	}

	Parent::serverCmdUseInventory(%client, %a);

	if (%override)
		%miniGame.EnableBuilding = %backupEnableBuilding;
}

function serverCmdUseSprayCan(%client, %a)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnablePainting = %miniGame.EnablePainting;
		%miniGame.EnablePainting = 1;
		%override = 1;
	}

	Parent::serverCmdUseSprayCan(%client, %a);

	if (%override)
		%miniGame.EnablePainting = %backupEnablePainting;
}

function serverCmdUseWand(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnableWand = %miniGame.EnableWand;
		%miniGame.EnableWand = 1;
		%override = 1;
	}

	Parent::serverCmdUseWand(%client);

	if (%override)
		%miniGame.EnableWand = %backupEnableWand;
}

function WandItem::onUse(%this, %player, %a)
{
	%client = %player.client;
	if (isObject(%client))
	{
		%miniGame = %client.miniGame;
		if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
		{
			%backupEnableWand = %miniGame.EnableWand;
			%miniGame.EnableWand = 1;
			%override = 1;
		}
	}

	Parent::onUse(%this, %player, %a);

	if (%override)
		%miniGame.EnableWand = %backupEnableWand;
}

function serverCmdNewDuplicator(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnableBuilding = %miniGame.EnableBuilding;
		%miniGame.EnableBuilding = 1;
		%override = 1;
	}

	Parent::serverCmdNewDuplicator(%client);

	if (%override)
		%miniGame.EnableBuilding = %backupEnableBuilding;
}

function Player::ndFired(%this)
{
	%client = %this.client;
	if (isObject(%client))
	{
		%miniGame = %client.miniGame;
		if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
		{
			%backupEnableBuilding = %miniGame.EnableBuilding;
			%miniGame.EnableBuilding = 1;
			%override = 1;
		}
	}

	Parent::ndFired(%this);

	if (%override)
		%miniGame.EnableBuilding = %backupEnableBuilding;
}

function serverCmdfillcan(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
	{
		%backupEnablePainting = %miniGame.EnablePainting;
		%miniGame.EnablePainting = 1;
		%override = 1;
	}

	Parent::serverCmdfillcan(%client);

	if (%override)
		%miniGame.EnablePainting = %backupEnablePainting;
}

function serverCmdAutobridge(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.duel)
		return;

	Parent::serverCmdAutobridge(%client);
}

function fillcanProjectile::onCollision(%this, %obj, %a, %b, %c, %d)
{
	%client = %obj.client;
	if (isObject(%client))
	{
		%miniGame = %client.miniGame;
		if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
		{
			%backupEnablePainting = %miniGame.EnablePainting;
			%miniGame.EnablePainting = 1;
			%override = 1;
		}
	}

	Parent::onCollision(%this, %obj, %a, %b, %c, %d);

	if (%override)
		%miniGame.EnablePainting = %backupEnablePainting;
}

}; // package Server_Dueling

package Server_Dueling_Deferred {

function serverCmdPlantBrick(%client)
{
	%partition = %client.partition;

	if (%partition && %partition.bricks.getCount() >= %partition.maxBricks)
	{
		messageClient(%client, 'MsgPlantError_Limit');
		return;
	}

	Parent::serverCmdPlantBrick(%client);

	if (%partition && %partition.plantErrorTooFar)
	{
		%partition.plantErrorTooFar = "";
		messageClient(%client, 'MsgPlantError_TooFar');
	}
}

}; // package Server_Dueling_Deferred
