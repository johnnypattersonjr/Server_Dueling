// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsWorldPartitionManagerSO()
{
	if (isObject(dsWorldPartitionManagerSO))
		return nameToID(dsWorldPartitionManagerSO);

	%obj = new ScriptObject(dsWorldPartitionManagerSO)
	{
		defaultMaxBricks = 20000;
		defaultMaxBricksPerBin = 2500;
	};

	%obj.freePartitions = new SimGroup();
	%obj.usedPartitions = new SimGroup();
	%obj.partitionPositionTable = new GuiTextListCtrl();

	// Force advanced environment mode so loaded map settings are never lost.
	if ($EnvGuiServer::SimpleMode)
	{
		$EnvGuiServer::SimpleMode = 0;
		EnvGuiServer::getIdxFromFilenames();
	}

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
	%defaultColorSet = dsColorSetManagerSO.defaultColorSet;

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
			colorSet = %defaultColorSet;
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

		%spectatorSpawnPoints = new SimSet();
		%obj.spectatorSpawnPoints = %spectatorSpawnPoints;
		%missionCleanup.add(%spectatorSpawnPoints);

		if (%i == 0)
		{
			%groundPlane = nameToID(groundPlane);
			%sky = nameToID(Sky);
			%sun = nameToID(Sun);
			%sunLight = nameToID(SunLight);

			%this.defaultGroundPlaneColor = %groundPlane.color;
			%this.defaultGroundPlaneScrollSpeed = %groundPlane.scrollSpeed;

			%this.defaultSkyColor = %sky.skyColor;
			%this.defaultSkyFogColor = %sky.fogColor;
			%this.defaultSkyFogDistance = %sky.fogDistance;
			%this.defaultSkyVisibleDistance = %sky.visibleDistance;

			%this.defaultSunAmbientColor = %sun.ambient;
			%this.defaultSunAzimuth = %sun.azimuth;
			%this.defaultSunDirectColor = %sun.color;
			%this.defaultSunElevation = %sun.elevation;
			%this.defaultSunShadowColor = %sun.shadowColor;

			%this.defaultSunLightColor = %sunLight.color;
			%this.defaultSunLightLocalPath = %sunLight.LocalFlareBitmap;
			%this.defaultSunLightRemotePath = %sunLight.RemoteFlareBitmap;
			%this.defaultSunLightSize = %sunLight.FlareSize;
		}
		else
		{
			%groundPlane = new fxPlane(:groundPlane);
			%groundPlane.bottomTexture = groundPlane.bottomTexture;
			%groundPlane.topTexture = groundPlane.topTexture;
			%sky = new Sky(:Sky);
			%sky.setName("");
			%sun = new Sun(:Sun);
			%sunLight = new fxSunLight(:SunLight);
			%missionCleanup.add(%groundPlane);
			%missionCleanup.add(%sky);
			%missionCleanup.add(%sun);
			%missionCleanup.add(%sunLight);
		}

		%sun.setScopeAlways();
		GhostAlwaysSet.remove(%sun);
		%obj.sun = %sun;

		%sky.setScopeAlways();
		GhostAlwaysSet.remove(%sky);
		%obj.sky = %sky;

		%sunLight.setScopeAlways();
		GhostAlwaysSet.remove(%sunLight);
		%obj.sunLight = %sunLight;

		%groundPlane.setScopeAlways();
		GhostAlwaysSet.remove(%groundPlane);
		%obj.groundPlane = %groundPlane;

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

	%partition = %freePartitions.getObject(getRandom(0, %count - 1));
	%this.usedPartitions.add(%partition);

	%partition.queueCreateBoundary();

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

function dsWorldPartitionManagerSO::queueRelease(%this, %partition)
{
	%frameQueue = nameToID(dsFrameQueueSO);
	%frameQueue.push("return " @ %partition @ ".dispatchClearBricks();");
	%frameQueue.push("return " @ %partition @ ".dispatchDeleteBoundary();");
	%frameQueue.push("return " @ %this @ ".schedule(1, finishRelease, " @ %partition @ ");");
	%frameQueue.process();
}

function dsWorldPartitionManagerSO::finishRelease(%this, %partition)
{
	%partition.pushEnvironment();
	EnvGuiServer::SetSimpleMode();
	EnvGuiServer::readAdvancedVarsFromSimple();
	EnvGuiServer::SetAdvancedMode();
	%partition.popEnvironment();

	%partition.setColorSet(dsColorSetManagerSO.defaultColorSet);

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
	if (!%client.isAdmin)
		commandToClient(%client, 'SetAdminLevel', -1);

	messageClient(%client, '', '\c5Mini-game created.');
	commandToClient(%client, 'SetPlayingMiniGame', 1);
	commandToClient(%client, 'SetRunningMiniGame', 1);
	commandToClient(%client, 'SetBuildingDisabled', 0);
	commandToClient(%client, 'SetPaintingDisabled', 0);
	commandToClient(%client, 'dcSetBrickCount', 0, %this.maxBricks);

	commandToAll('AddMiniGameLine', %miniGame.getLine(), %miniGame, %colorIdx);

	dsChallengeManagerSO.broadcastPlayerUpdate(%client, 2, %client);

	%miniGame.lastResetTime = 0;
	%miniGame.reset(%client);

	// NOTE: Backups are auto-loaded for now, so people without the client can build without losing progress.
	// commandToClient(%client, 'dcLoadBackup');

	if (isFile(%path = dsMapManagerSO.directory @ %client.bl_id @ "/.backup.bls"))
		%miniGame.partition.queueLoadBricks(%path);

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

	%miniGame.messageAll('', "\c2[FT" @ %miniGame.goal @ ",WB2] \c3" @ %miniGame.duelistName1 @ " \c2[" @ %miniGame.duelistScore1 @ " - " @ %miniGame.duelistScore2 @ "] \c3" @ %miniGame.duelistName2);

	if (%map)
	{
		%name = %map.name;
		%directory = dsMapManagerSO.directory @ %map.submitterID @ "/";
		%frameQueue = nameToID(dsFrameQueueSO);
		%path = %directory @ %name @ "-bricks.bls";
		%miniGame.messageAll('', "\c2Loading map \c3\"" @ %name @ "\"\c2...");

		%this.loadEnvironment(%map);
		if (%colorSet = dsColorSetManagerSO.findColorSet(%map.colorSet))
			%frameQueue.push("return " @ %this @ ".schedule(1, setColorSet, " @ %colorSet @ ");");
		%this.queueLoadBricks(%path);
		%frameQueue.push("return " @ nameToID(dsMapManagerSO) @ ".schedule(1, onLoadBricksFinished, " @ %this @ ", " @ %map @ ");");
		%frameQueue.push("return " @ %this @ ".schedule(1, onMapLoadFinished, " @ %map @ ", " @ %miniGame @ ");");
		%frameQueue.process();

		%miniGame.duelReady = 0;
		%miniGame.duelStarted = "";
		%miniGame.lastResetTime = 0;
		%miniGame.reset(%duelist1);
	}
	else
	{
		%miniGame.duelReady = 1;
		%miniGame.startDuel(%duelist1);
	}

	if (!%duelist1.isAIControlled())
		commandToClient(%duelist1, 'dcCloseWindow');
	if (!%duelist2.isAIControlled())
		commandToClient(%duelist2, 'dcCloseWindow');

	return 1;
}

function dsWorldPartitionSO::onMapLoadFinished(%this, %map, %miniGame)
{
	if (!isObject(%miniGame))
		return;

	%miniGame.duelReady = 1;
	%miniGame.schedule(5000, startDuel, %miniGame.duelist1);
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

	%db = nameToID(%brick.getDataBlock());

	if (%db == nameToID(brickSpawnPointData) ||
		%db == nameToID(brickDiagonalSpawnPointData)) // Brick_DiagonalSpawn
	{
		%this.spawnPoints.add(%brick);
	}
	else if (%db == nameToID(brickSpectatorSpawnPointData)) // Server_Dueling
	{
		%this.spectatorSpawnPoints.add(%brick);
	}

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

function dsWorldPartitionSO::queueCreateBoundary(%this)
{
	%frameQueue = nameToID(dsFrameQueueSO);
	%frameQueue.push("return " @ %this @ ".schedule(1, createBoundaryXWall1);");
	%frameQueue.push("return " @ %this @ ".schedule(1, createBoundaryXWall2);");
	%frameQueue.push("return " @ %this @ ".schedule(1, createBoundaryYWall1);");
	%frameQueue.push("return " @ %this @ ".schedule(1, createBoundaryYWall2);");
	%frameQueue.push("return " @ %this @ ".schedule(1, createBoundaryRoof);");
	%frameQueue.process();
}

function dsWorldPartitionSO::createBoundary(%this)
{
	// Assume the following:
	//  - Partition size is a multiple of 32
	//  - Boundary size is 32
	//  - Partition size encapsulates the boundary

	%this.createBoundaryXWall1();
	%this.createBoundaryXWall2();
	%this.createBoundaryYWall1();
	%this.createBoundaryYWall2();
	%this.createBoundaryRoof();
}

function dsWorldPartitionSO::createBoundaryXWall1(%this)
{
	%brickGroup = nameToID(BrickGroup_888888);
	%dataBlock = nameToID(brick64xBoundaryCubeData);
	%partitionSize = dsWorldPartitionManagerSO.partitionSize;
	%boundary = %this.boundary;
	%origin = %this.position;

	%end = (%partitionSize - 32) / 2;
	%start = -1 * %end;
	%wallEnd = %end - 32;
	%wallStart = -1 * %wallEnd;
	%wallVertEnd = %partitionSize - 48;
	%roofZ = %partitionSize - 16;

	%instantGroupBackup = $instantGroup;
	$instantGroup = %brickGroup;

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
		%brick.trustCheckFinished();
		%brick.plant();
	}

	$instantGroup = %instantGroupBackup;
}

function dsWorldPartitionSO::createBoundaryXWall2(%this)
{
	%brickGroup = nameToID(BrickGroup_888888);
	%dataBlock = nameToID(brick64xBoundaryCubeData);
	%partitionSize = dsWorldPartitionManagerSO.partitionSize;
	%boundary = %this.boundary;
	%origin = %this.position;

	%end = (%partitionSize - 32) / 2;
	%start = -1 * %end;
	%wallEnd = %end - 32;
	%wallStart = -1 * %wallEnd;
	%wallVertEnd = %partitionSize - 48;
	%roofZ = %partitionSize - 16;

	%instantGroupBackup = $instantGroup;
	$instantGroup = %brickGroup;

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
		%brick.trustCheckFinished();
		%brick.plant();
	}

	$instantGroup = %instantGroupBackup;
}

function dsWorldPartitionSO::createBoundaryYWall1(%this)
{
	%brickGroup = nameToID(BrickGroup_888888);
	%dataBlock = nameToID(brick64xBoundaryCubeData);
	%partitionSize = dsWorldPartitionManagerSO.partitionSize;
	%boundary = %this.boundary;
	%origin = %this.position;

	%end = (%partitionSize - 32) / 2;
	%start = -1 * %end;
	%wallEnd = %end - 32;
	%wallStart = -1 * %wallEnd;
	%wallVertEnd = %partitionSize - 48;

	%instantGroupBackup = $instantGroup;
	$instantGroup = %brickGroup;

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
		%brick.trustCheckFinished();
		%brick.plant();
	}

	$instantGroup = %instantGroupBackup;
}

function dsWorldPartitionSO::createBoundaryYWall2(%this)
{
	%brickGroup = nameToID(BrickGroup_888888);
	%dataBlock = nameToID(brick64xBoundaryCubeData);
	%partitionSize = dsWorldPartitionManagerSO.partitionSize;
	%boundary = %this.boundary;
	%origin = %this.position;

	%end = (%partitionSize - 32) / 2;
	%start = -1 * %end;
	%wallEnd = %end - 32;
	%wallStart = -1 * %wallEnd;
	%wallVertEnd = %partitionSize - 48;

	%instantGroupBackup = $instantGroup;
	$instantGroup = %brickGroup;

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
		%brick.trustCheckFinished();
		%brick.plant();
	}

	$instantGroup = %instantGroupBackup;
}

function dsWorldPartitionSO::createBoundaryRoof(%this)
{
	%brickGroup = nameToID(BrickGroup_888888);
	%dataBlock = nameToID(brick64xBoundaryCubeData);
	%partitionSize = dsWorldPartitionManagerSO.partitionSize;
	%boundary = %this.boundary;
	%origin = %this.position;

	%end = (%partitionSize - 32) / 2;
	%start = -1 * %end;
	%roofZ = %partitionSize - 16;

	%instantGroupBackup = $instantGroup;
	$instantGroup = %brickGroup;

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
		%brick.trustCheckFinished();
		%brick.plant();
	}

	$instantGroup = %instantGroupBackup;
}

function dsWorldPartitionSO::dispatchDeleteBoundary(%this)
{
	%boundary = %this.boundary;
	%count = %boundary.getCount();
	if (!%count)
		return 0;

	%some = 32;

	for (%i = 0; %i < %count; %i += %some)
	{
		if (%i + %some >= %count)
			%event = %this.schedule(0, deleteSomeBoundary, -1);
		else
			%event = %this.schedule(0, deleteSomeBoundary, %some);
	}

	return %event;
}

function dsWorldPartitionSO::deleteSomeBoundary(%this, %some)
{
	%boundary = %this.boundary;
	%count = %boundary.getCount();
	if (!%count)
		return;

	%count = %some < 0 ? %count : getMin(%count, %some);
	for (%i = 0; %i < %count; %i++)
		%boundary.getObject(%boundary.getCount() - 1).delete();
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

function dsWorldPartitionSO::scopeEnvironment(%this, %client)
{
	%this.groundPlane.scopeToClient(%client);
	%this.sky.scopeToClient(%client);
	%this.sun.scopeToClient(%client);
	%this.sunLight.scopeToClient(%client);
}

function dsWorldPartitionSO::pushEnvironment(%this)
{
	if (%this == dsWorldPartitionManagerSO.centerPartition)
		return;

	%this.groundPlane.setName("groundPlane");
	%this.sky.setName("Sky");
	%this.sun.setName("Sun");
	%this.sunLight.setName("SunLight");
}

function dsWorldPartitionSO::popEnvironment(%this)
{
	if (%this == dsWorldPartitionManagerSO.centerPartition)
		return;

	%this.groundPlane.setName("");
	%this.sky.setName("");
	%this.sun.setName("");
	%this.sunLight.setName("");
}

function dsWorldPartitionSO::saveSomeBricks(%this, %saveOwners, %saveWorldBox, %some)
{
	%bricks = %this.bricks;
	%count = %bricks.getCount();
	if (!%count)
		return;

	%file = %this.saveBricksFile;
	%start = %this.saveBricksIdx;

	%count = %some < 0 ? (%count - %start) : getMin(%count - %start, %some);
	%end = %start + %count;
	%origin = %this.position;
	%this.saveBricksIdx = %end;

	%owners = %this.saveOwnersList;

	for (%i = %start; %i < %end; %i++)
	{
		%brick = %bricks.getObject(%i);
		%db = %brick.getDataBlock();

		if (%printID = %brick.printID)
		{
			%texture = getPrintTexture(%printID);

			if (%texture $= "base/data/shapes/bricks/brickTop.png")
			{
				%texture = "/";
			}
			else
			{
				%aspectStart = 14; // strlen("Add-Ons/Print_")
				%aspectEnd = strpos(%texture, "_", %aspectStart + 1);
				%aspectRatio = getSubStr(%texture, %aspectStart, %aspectEnd - %aspectStart);

				%texture = %aspectRatio @ "/" @ fileBase(%texture);
			}
		}
		else
		{
			%texture = "";
		}

		%file.writeLine(%db.uiName @ "\" " @ VectorSub(%brick.position, %origin) SPC %brick.getAngleID() SPC %brick.isBasePlate() SPC %brick.getColorID() SPC %texture SPC %brick.getColorFxID() SPC %brick.getShapeFxID() SPC %brick.isRayCasting() SPC %brick.isColliding() SPC %brick.isRendering());

		%brickGroup = %brick.getGroup();
		%bl_id = %brickGroup.bl_id;
		%file.writeLine("+-OWNER " @ %bl_id);
		if ((%name = %brick.getName()) !$= "")
			%file.writeLine("+-NTOBJECTNAME " @ %name);

		%itemDirection = %brick.itemDirection;
		%itemRespawnTime = %brick.itemRespawnTime;

		if (isObject(%audio = %brick.AudioEmitter))
			%file.writeLine("+-AUDIOEMITTER " @ %audio.getProfileId().uiName @ "\"");
		if ((%emitter = nameToID(%brick.emitter)) != -1 || %brick.emitterDirection)
			%file.writeLine("+-EMITTER " @ (%emitter != -1 ? %emitter.getEmitterDataBlock().uiName : "NONE") @ "\" " @ %brick.emitterDirection);
		if ((%item = nameToID(%brick.item)) != -1 || (%itemDirection !$= "" && %itemDirection != 2) || %brick.itemPosition || (%itemRespawnTime && %itemRespawnTime != 4000))
			%file.writeLine("+-ITEM " @ (%item != -1 ? %item.getDataBlock().uiName : "NONE") @ "\" " @ %brick.itemPosition SPC %itemDirection SPC %itemRespawnTime);
		if (isObject(%light = %brick.light))
			%file.writeLine("+-LIGHT " @ %light.getDataBlock().uiName @ "\" " @ %light.Enable);
		if (isObject(%vehicleSpawn = %brick.VehicleSpawnMarker))
			%file.writeLine("+-VEHICLE " @ %vehicleSpawn.getUiName() @ "\" " @ %vehicleSpawn.getReColorVehicle());

		%numEvents = %brick.numEvents;
		for (%j = 0; %j < %numEvents; %j++)
		{
			%inputIdx = %brick.eventInputIdx[%j];
			%inputName = $InputEvent_Name["fxDTSBrick", %inputIdx];

			%targetIdx = %brick.eventTargetIdx[%j];
			if (%targetIdx != -1)
			{
				%target = getField($InputEvent_TargetList["fxDTSBrick", %inputIdx], %targetIdx);

				%targetBrickName = "";
				%targetClass = getWord(%target, 1);
				%targetName = getWord(%target, 0);
			}
			else
			{
				%targetBrickName = %brick.eventNT[%j];
				%targetClass = "fxDTSBrick";
				%targetName = -1;
			}

			%outputIdx = %brick.eventOutputIdx[%j];
			%outputName = $OutputEvent_Name[%targetClass, %outputIdx];

			%outputParameterList = $OutputEvent_parameterList[%targetClass, %outputIdx];
			%params = "";
			for (%k = 0; %k < 4; %k++)
			{
				%outputParameter = %brick.eventOutputParameter[%j, %k + 1];

				if (getWord(getField(%outputParameterList, %k), 0) $= "dataBlock")
				{
					%params = %params TAB (isObject(%outputParameter) ? %outputParameter.getName() : -1);
				}
				else
				{
					%params = %params TAB %outputParameter;
				}
			}

			%file.writeLine("+-EVENT\t" @ %j TAB %brick.eventEnabled[%j] TAB %inputName TAB %brick.eventDelay[%j] TAB %targetName TAB %targetBrickName TAB %outputName @ %params);
		}

		if (%saveOwners && %owners.getRowNumById(%bl_id) < 0)
			%owners.addRow(%bl_id, StripMLControlChars(%brickGroup.name));
	}

	if (%saveWorldBox)
	{
		%minX = %this.saveMinX;
		%minY = %this.saveMinY;
		%minZ = %this.saveMinZ;
		%maxX = %this.saveMaxX;
		%maxY = %this.saveMaxY;
		%maxZ = %this.saveMaxZ;

		for (%i = %start; %i < %end; %i++)
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

		%this.saveMinX = %minX;
		%this.saveMinY = %minY;
		%this.saveMinZ = %minZ;
		%this.saveMaxX = %maxX;
		%this.saveMaxY = %maxY;
		%this.saveMaxZ = %maxZ;
	}
}

function dsWorldPartitionSO::queueLoadBricks(%this, %path, %offset, %announce)
{
	%announce = %announce | 0;

	// TODO: Lock down building states

	%frameQueue = nameToID(dsFrameQueueSO);
	%frameQueue.push(%this @ ".schedule(1, loadBricksStart, \"" @ %path @ "\", \"" @ %offset @ "\", " @ %announce @ "); " @ %this @ ".loadBricksFinishedEvent = " @ %this @ ".schedule(2147483647, loadBricksFinished, " @ %announce @ "); return " @ %this @ ".loadBricksFinishedEvent;");
	%frameQueue.process();
}

function dsWorldPartitionSO::loadBricksStart(%this, %path, %offset, %announce)
{
	%file = new FileObject();
	if (!%file.openForRead(%path))
	{
		cancel(%this.loadBricksFinishedEvent);
		%this.loadBricksFinishedEvent = "";
		error("Could not load bricks from <" @ %path @ ">.");
		%file.delete();
		return;
	}

	%file.readLine();
	%count = %file.readLine();
	for (%i = 0; %i < %count; %i++)
		%file.readLine();
	for (%i = 0; %i < 64; %i++)
		%file.readLine();

	%line = %file.readLine();
	if (strstr(%line, "Linecount ") != 0)
	{
		cancel(%this.loadBricksFinishedEvent);
		%this.loadBricksFinishedEvent = "";
		error("Could not load bricks from <" @ %path @ ">.");
		%file.close();
		%file.delete();
		return;
	}

	%this.loadBricksFile = %file;
	%this.loadBricksExpectedCount = getWord(%line, 1);
	%this.loadBricksCount = 0;
	%this.loadBricksTickEvent = %this.schedule(0, loadBricksTick, %path, %offset, %announce);
}

function dsWorldPartitionSO::loadBricksTick(%this, %path, %offset, %announce)
{
	// TODO: Handle color transition if necessary (set LoadingBricks_Client, loading color mode to 3, and color transition table so the events also set the correct color)

	cancel(%this.loadBricksTickEvent);

	%file = %this.loadBricksFile;
	%some = 4;

	%nextLine = %this.loadLine;

	%lastLoadedBrickBackup = $LastLoadedBrick;
	%loadingBricks_ClientBackup = $LoadingBricks_Client;
	%server_LoadFileObjBackup = $Server_LoadFileObj;

	$LoadingBricks_Client = "";
	%i = 0;

	%rotation[0] = "1 0 0 0";
	%rotation[1] = "0 0 1 90";
	%rotation[2] = "0 0 1 180";
	%rotation[3] = "0 0 -1 90";

	while (%i < %some)
	{
		if (%nextLine !$= "")
		{
			%line = %nextLine;
			%nextLine = "";
		}
		else if (%file.isEOF())
		{
			%finished = 1;
			break;
		}
		else
		{
			%line = %file.readLine();
		}

		if (%line $= "" || strstr(%line, "+-") == 0)
			continue;

		%uiNameEnd = strstr(%line, "\"");
		// TODO: Handle if (%uiNameEnd == -1)
		%uiName = getSubStr(%line, 0, %uiNameEnd);
		%line = getSubStr(%line, %uiNameEnd + 2, 1e5);

		if (!isObject(%dataBlock = $uiNameTable[%uiName]))
		{
			while (true)
			{
				if (%file.isEOF())
				{
					%finished = 1;
					break;
				}

				%line = %file.readLine();

				if (strstr(%line, "+-") != 0)
				{
					if (strstr(%line, "\"") != -1)
						%nextLine = %line;

					break;
				}
			}

			%i++;

			if (%finished)
				break;

			continue;
		}

		%position = getWords(%line, 0, 2);
		%angleID = mClamp(getWord(%line, 3), 0, 3);
		%isBasePlate = getWord(%line, 4);
		%colorID = getWord(%line, 5);
		%texture = getWord(%line, 6);
		%colorFxID = getWord(%line, 7);
		%shapeFxID = getWord(%line, 8);
		%isRayCasting = getWord(%line, 9);
		%isColliding = getWord(%line, 10);
		%isRendering = getWord(%line, 11);

		%printID = $printNameTable[%texture];
		if (%printID $= "")
			%printID = 0;

		%brick = new fxDTSBrick()
		{
			dataBlock = %dataBlock;

			angleID = %angleID;
			colorFxID = %colorFxID;
			colorID = %colorID;
			isBasePlate = %isBasePlate;
			isPlanted = 1;
			position = VectorAdd(VectorAdd(%position, %this.position), %offset);
			rotation = %rotation[%angleID];
			shapeFxID = %shapeFxID;
			printID = %printID;
		};
		BrickGroup_888888.add(%brick);

		$LastLoadedBrick = %brick;
		$Server_LoadFileObj = %brick;

		%plant = 1;

		while (true)
		{
			%line = %file.readLine();

			if (strstr(%line, "+-") == 0)
			{
				if (%brick $= "")
					continue;

				%type = getSubStr(getWord(%line, 0), 2, 1e5);

				if (%type $= "OWNER")
				{
					// Expected to be first line of extended info.
					%bl_id = getWord(%line, 1);
					%brickGroup = "BrickGroup_" @ %bl_id;
					if (!isObject(%brickGroup))
					{
						%brickGroup = new SimGroup(%brickGroup)
						{
							bl_id = %bl_id;
							name = "\c1BL_ID: " @ %bl_id @ "\c0";
						};
						mainBrickGroup.add(%brickGroup);
					}

					%brickGroup.add(%brick);
					%brick.stackBL_ID = %bl_id;
				}

				if (%plant)
				{
					%plant = "";
					%brick.trustCheckFinished();

					%result = %brick.plant();
					if (%result != 0)
					{
						%brick.delete();
						%brick = "";
						continue;
					}

					%brick.setColliding(%isColliding);
					%brick.setRayCasting(%isRayCasting);
					%brick.setRendering(%isRendering);

					%loaded++;

					if (%type $= "OWNER")
						continue;
				}
				// TODO: Handle logic for transitioning brick to new owner after plant.
				// else if (%type $= "OWNER")
				// {
				// }

				switch$ (%type)
				{
				case "AUDIOEMITTER":
					%uiNameEnd = strstr(%line, "\"");
					// TODO: Handle if (%uiNameEnd == -1)
					%uiName = getSubStr(%line, 15, %uiNameEnd - 15);

					if (isObject(%db = $uiNameTable_Music[%uiName]))
						%brick.setSound(%db);
				case "EMITTER":
					%uiNameEnd = strstr(%line, "\"");
					// TODO: Handle if (%uiNameEnd == -1)
					%uiName = getSubStr(%line, 10, %uiNameEnd - 10);

					if (isObject(%db = $uiNameTable_Emitters[%uiName]))
						%brick.setEmitter(%db);

					%brick.setEmitterDirection(getSubStr(%line, %uiNameEnd + 2, 1e5));
				case "EVENT":
					%enabled = getField(%line, 2);
					%inputName = getField(%line, 3);
					%delay = getField(%line, 4);
					%targetName = getField(%line, 5);
					%targetBrickName = getField(%line, 6);
					%outputName = getField(%line, 7);
					%outputParameter1 = getField(%line, 8);
					%outputParameter2 = getField(%line, 9);
					%outputParameter3 = getField(%line, 10);
					%outputParameter4 = getField(%line, 11);

					%inputIdx = inputEvent_GetInputEventIdx(%inputName);
					%targetIdx = inputEvent_GetTargetIndex("fxDTSBrick", %inputIdx, %targetName);
					%targetClass = %targetName == -1 ? "fxDTSBrick" : getWord(getField($InputEvent_TargetList["fxDTSBrick", %inputIdx], %targetIdx), 1);
					%outputIdx = outputEvent_GetOutputEventIdx(%targetClass, %outputName);

					%brickGroup = %brick.getGroup();
					%brickGroup.wrenchBrick = %brick;
					serverCmdAddEvent(%brickGroup, %enabled, %inputIdx, %delay, %targetIdx, -1, %outputIdx,
					                  %outputParameter1, %outputParameter2, %outputParameter3, %outputParameter4);
					%brick.eventNT[%brick.numEvents - 1] = %targetBrickName;
					%brickGroup.wrenchBrick = "";
				case "ITEM":
					%uiNameEnd = strstr(%line, "\"");
					// TODO: Handle if (%uiNameEnd == -1)
					%uiName = getSubStr(%line, 7, %uiNameEnd - 7);
					%line = getSubStr(%line, %uiNameEnd + 2, 1e5);

					if (isObject(%db = $uiNameTable_Items[%uiName]))
						%brick.setItem(%db);

					%brick.setItemPosition(getWord(%line, 0));
					%brick.setItemDirection(getWord(%line, 1));
					%brick.setItemRespawnTime(getWord(%line, 2));
				case "LIGHT":
					%uiNameEnd = strstr(%line, "\"");
					// TODO: Handle if (%uiNameEnd == -1)
					%uiName = getSubStr(%line, 8, %uiNameEnd - 8);

					if (isObject(%db = $uiNameTable_Lights[%uiName]))
					{
						%brick.setLight(%db);
						%brick.light.setEnable(getSubStr(%line, %uiNameEnd + 2, 1e5));
					}
				case "NTOBJECTNAME":
					%name = getWord(%line, 1);
					%brick.setNTObjectName(%name);
				case "VEHICLE":
					%uiNameEnd = strstr(%line, "\"");
					// TODO: Handle if (%uiNameEnd == -1)
					%uiName = getSubStr(%line, 10, %uiNameEnd - 10);

					if (isObject(%db = $uiNameTable_Vehicle[%uiName]))
						%brick.setVehicle(%db);

					%brick.setReColorVehicle(getSubStr(%line, %uiNameEnd + 3, 1e5));
				}
			}
			else
			{
				%nextLine = %line;
				break;
			}
		}

		%i++;
	}

	$LastLoadedBrick = %lastLoadedBrickBackup;
	$LoadingBricks_Client = %loadingBricks_ClientBackup;
	$Server_LoadFileObj = %server_LoadFileObjBackup;

	%this.loadBricksCount += %loaded;

	if (%finished)
	{
		cancel(%this.loadBricksFinishedEvent);
		%this.loadBricksFinishedEvent = %this.schedule(1, loadBricksFinished, %path, %offset, %announce);
	}
	else
	{
		%this.loadLine = %nextLine;
		%this.loadBricksTickEvent = %this.schedule(0, loadBricksTick, %path, %offset, %announce);
	}
}

function dsWorldPartitionSO::loadBricksFinished(%this, %path, %offset, %announce)
{
	if (%announce && isObject(%miniGame = %this.miniGame))
		%miniGame.messageAll('', "\c2Finished loading the bricks. (" @ %this.loadBricksCount @ "/" @ %this.loadBricksExpectedCount @ ")");

	%file = %this.loadBricksFile;
	%file.close();
	%file.delete();

	%this.loadingBricks = "";

	%this.loadBricksCount = "";
	%this.loadBricksExpectedCount = "";
	%this.loadBricksFile = "";
	%this.loadBricksFinishedEvent = "";
	%this.loadBricksTickEvent = "";
}

function dsWorldPartitionSO::queueSaveBricks(%this, %path, %saveOwners, %saveWorldBox, %announce)
{
	// TODO: Consider deferring to a start function

	%bricks = %this.bricks;
	%count = %bricks.getCount();
	if (!%count)
		return 0;

	%file = new FileObject();
	if (!%file.openForWrite(%path))
	{
		error("Could not save bricks to <" @ %path @ ">.");
		%file.delete();
		return 0;
	}

	%saveOwners = %saveOwners | 0;
	%saveWorldBox = %saveWorldBox | 0;
	%announce = %announce | 0;
	%args = %saveOwners @ ", " @ %saveWorldBox;

	%frameQueue = nameToID(dsFrameQueueSO);
	%some = 256;

	if (%saveWorldBox)
	{
		%this.saveMinX = inf;
		%this.saveMinY = inf;
		%this.saveMinZ = inf;
		%this.saveMaxX = -inf;
		%this.saveMaxY = -inf;
		%this.saveMaxZ = -inf;
	}

	if (%saveOwners)
		%this.saveOwnersList = new GuiTextListCtrl();

	%file.writeLine("This is a Blockland save file.  You probably shouldn't modify it cause you'll screw it up.");
	%file.writeLine("1");
	%file.writeLine("");
	%colorSet = %this.colorSet;
	%colorCount = %colorSet.colorCount;

	for (%i = 0; %i < %colorCount; %i++)
		%file.writeLine(%colorSet.color[%i]);
	for (%i = %colorCount; %i < 64; %i++)
		%file.writeLine("1.000000 0.000000 1.000000 0.00000");

	// NOTE: This is just an initial estimate and the .bls file can contain a different amount of bricks.
	%file.writeLine("Linecount " @ %count);

	%this.saveBricksFile = %file;
	%this.saveBricksIdx = 0;

	for (%i = 0; %i < %count; %i += %some)
	{
		if (%i + %some >= %count)
			%frameQueue.push("return " @ %this @ ".schedule(1, saveSomeBricks, " @ %args @ ", -1);");
		else
			%frameQueue.push("return " @ %this @ ".schedule(1, saveSomeBricks, " @ %args @ ", " @ %some @ ");");
	}

	%frameQueue.push("return " @ %this @ ".schedule(1, saveBricksFinished, " @ %args @ ", " @ %announce @ ");");
	%frameQueue.process();

	return 1;
}

function dsWorldPartitionSO::saveBricksFinished(%this, %saveOwners, %saveWorldBox, %announce)
{
	if (%saveOwners)
	{
		if (%this.saveBricksIdx)
		{
			%owners = %this.saveOwnersList;
			%owners.sort(0, 1);
			%ownerCount = %owners.rowCount(); // Should always be at least 1

			%ownerString = %owners.getRowId(0) SPC %owners.getRowText(0);

			for (%i = 1; %i < %ownerCount; %i++)
				%ownerString = %ownerString TAB %owners.getRowId(%i) SPC %owners.getRowText(%i);

			%this.saveOwners = %ownerString;
		}

		%this.saveOwnersList.delete();
		%this.saveOwnersList = "";
	}

	if (%saveWorldBox)
		%this.saveWorldBox = %this.saveMinX SPC %this.saveMinY SPC %this.saveMinZ SPC %this.saveMaxX SPC %this.saveMaxY SPC %this.saveMaxZ;

	if (%announce && isObject(%miniGame = %this.miniGame))
	{
		if (%this.saveBricksIdx)
			%miniGame.messageAll('', "\c2Finished saving the bricks.");
		else
			%miniGame.messageAll('', "\c0No bricks found.");
	}

	%file = %this.saveBricksFile;
	%file.close();
	%file.delete();

	%this.saveBricksFile = "";
	%this.saveBricksIdx = "";
	%this.savingBricks = "";
}

function dsWorldPartitionSO::setColorSet(%this, %colorSet, %announce, %noUpdate, %noGhostingReset)
{
	// Clients can get the console error "getColorIDTable() - index 'XX' out of range" when they
	// have colors selected in the last row and the new colorset does not have colors at those
	// same row-level indices or the number of colors per row increases.
	// TODO: Use a client command to set the current color swatch selection to something in range.

	if (%this.colorSet == %colorSet)
		return;

	if (%noUpdate)
	{
		%this.colorSet = %colorSet;
		return;
	}

	if (isObject(%miniGame = %this.miniGame))
	{
		%numMembers = %miniGame.numMembers;
		for (%i = 0; %i < %numMembers; %i++)
			%miniGame.member[%i].resetGhosting();

		%colorSet.apply();

		for (%i = 0; %i < %numMembers; %i++)
		{
			%member = %miniGame.member[%i];
			%member.transmitStaticBrickData();
			%member.activateGhosting();
			%member.onActivateGhosting();
			%this.scopeEnvironment(%member);
		}

		if (%miniGame.buildSession)
			%miniGame.commandToAll('PlayGui_LoadPaint');

		if (%announce)
			%miniGame.messageAll('', "\c2Changed color set to \c3" @ %colorSet.name @ "\c2!");
	}
	else if (%this == dsWorldPartitionManagerSO.centerPartition)
	{
		%group = nameToID(ClientGroup);
		%count = %group.getCount();

		for (%i = 0; %i < %count; %i++)
		{
			%client = %group.getObject(%i);
			if (!%client.hasSpawnedOnce || %client.isAIControlled() || isObject(%client.miniGame))
				continue;

			%client.resetGhosting();
		}

		%colorSet.apply();

		for (%i = 0; %i < %count; %i++)
		{
			%client = %group.getObject(%i);
			if (!%client.hasSpawnedOnce || %client.isAIControlled() || isObject(%client.miniGame))
				continue;

			%client.transmitStaticBrickData();
			%client.activateGhosting();
			%client.onActivateGhosting();
			%this.scopeEnvironment(%client);
			commandToClient(%client, 'PlayGui_LoadPaint');

			if (%announce)
				messageClient(%client, '', "\c2Changed color set to \c3" @ %colorSet.name @ "\c2!");
		}
	}

	%this.colorSet = %colorSet;
}

function dsWorldPartitionSO::positionInside(%this, %position)
{
	%x = getWord(%worldBox, 0);
	%y = getWord(%worldBox, 1);
	%z = getWord(%worldBox, 2);

	return %x <= %this.maxX && %y <= %this.maxY && %z <= %this.maxZ && %x >= %this.minX && %y >= %this.minY && %z >= %this.minZ;
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

function dsWorldPartitionSO::queueCenterBricks(%this, %announce)
{
	%bricks = %this.bricks;
	if (!%bricks.getCount())
		return 0;

	%path = dsMapManagerSO.directory @ ".temp-centerBricks.bls";
	if (!%this.queueSaveBricks(%path, 0, 1))
		return 0;

	%frameQueue = nameToID(dsFrameQueueSO);
	%frameQueue.push("return " @ %this @ ".schedule(1, centerBricksIntermediate, " @ (%announce | 0) @ ");");
	%frameQueue.process();

	return 1;
}

function dsWorldPartitionSO::centerBricksIntermediate(%this, %announce)
{
	%center = getBoxCenter(%this.saveWorldBox);
	%offset = setWord(VectorScale(%center, -1), 2, 0);

	// Offset must be a multiple of 0.5
	%offset = VectorScale(%offset, 2);
	%offset = (getWord(%offset, 0) | 0) SPC (getWord(%offset, 1) | 0) SPC (getWord(%offset, 2) | 0);
	%offset = VectorScale(%offset, 0.5);

	if (VectorLen(%offset) < 1)
	{
		if (%announce && isObject(%miniGame = %this.miniGame))
			%miniGame.messageAll('', "\c2The bricks are already centered.");

		return;
	}

	if (!%this.queueClearBricks())
	{
		if (%announce && isObject(%miniGame = %this.miniGame))
			%miniGame.messageAll('', "\c2There are no bricks to center.");

		return;
	}

	%path = dsMapManagerSO.directory @ ".temp-centerBricks.bls";
	%this.queueLoadBricks(%path, %offset);

	%frameQueue = nameToID(dsFrameQueueSO);
	%frameQueue.push("return " @ %this @ ".schedule(1, centerBricksFinished, " @ %announce @ ");");
	%frameQueue.process();
}

function dsWorldPartitionSO::centerBricksFinished(%this, %announce)
{
	if (%announce && isObject(%miniGame = %this.miniGame))
		%miniGame.messageAll('', "\c2Finished centering the bricks.");
}

function dsWorldPartitionSO::clearSomeBricks(%this, %some)
{
	%bricks = %this.bricks;
	%count = %bricks.getCount();
	if (!%count)
		return;

	%count = %some < 0 ? %count : getMin(%count, %some);
	for (%i = 0; %i < %count; %i++)
	{
		%brick = %bricks.getObject(%bricks.getCount() - 1);
		%brick.onDeath();
		%brick.delete();
	}
}

function dsWorldPartitionSO::dispatchClearBricks(%this, %announce)
{
	%bricks = %this.bricks;
	%count = %bricks.getCount();
	if (!%count)
		return 0;

	%some = 128;

	for (%i = 0; %i < %count; %i += %some)
	{
		if (%i + %some >= %count)
			%this.schedule(0, clearSomeBricks, -1);
		else
			%this.schedule(0, clearSomeBricks, %some);
	}

	return %this.schedule(0, clearBricksFinished, %announce);
}

function dsWorldPartitionSO::queueClearBricks(%this, %announce)
{
	%bricks = %this.bricks;
	%count = %bricks.getCount();
	if (!%count)
		return 0;

	%frameQueue = nameToID(dsFrameQueueSO);
	%some = 256;

	for (%i = 0; %i < %count; %i += %some)
	{
		if (%i + %some >= %count)
			%frameQueue.push("return " @ %this @ ".schedule(1, clearSomeBricks, -1);");
		else
			%frameQueue.push("return " @ %this @ ".schedule(1, clearSomeBricks," @ %some @ ");");
	}

	%frameQueue.push("return " @ %this @ ".schedule(1, clearBricksFinished, " @ (%announce | 0) @ ");");
	%frameQueue.process();

	return 1;
}

function dsWorldPartitionSO::clearBricksFinished(%this, %announce)
{
	if (%announce && isObject(%miniGame = %this.miniGame))
		%miniGame.messageAll('', "\c2Finished clearing the bricks.");

	%this.reportBricks();
	%this.clearingBricks = "";

	// Skip possible false negative hang detection frame.
	if ($detectHangLastRealTime !$= "")
		$detectHangLastRealTime = getRealTime();
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

	return isObject(%spawnPoint) ? %spawnPoint.getSpawnPoint() : VectorAdd(pickSpawnPoint(), %this.position);
}

function dsWorldPartitionSO::pickSpectatorSpawnPoint(%this)
{
	%spawnPoints = %this.spectatorSpawnPoints;
	%count = %spawnPoints.getCount();

	if (%count)
		%spawnPoint = %spawnPoints.getObject(getRandom(0, %count - 1));

	return isObject(%spawnPoint) ? %spawnPoint.getSpawnPoint() : VectorAdd(pickSpawnPoint(), %this.position);
}

function Observer::onTrigger(%this, %obj, %trigger, %state)
{
	if (!%state || %trigger != 0)
	{
		Parent::onTrigger(%this, %obj, %trigger, %state);
		return;
	}

	%client = %obj.getControllingClient();
	if (isObject(%miniGame = %client.miniGame) && %miniGame.duel &&
		%miniGame.partition.spectatorSpawnPoints.getCount() && // Important, or will otherwise lead to an infinite loop due to current method to set camera orbit mode.
		%client != %miniGame.duelist1 && %client != %miniGame.duelist2 &&
		(!isObject(%client.player) || %client.player.getDamagePercent() >= 1))
	{
		%client.spawnPlayer();
		return;
	}

	Parent::onTrigger(%this, %obj, %trigger, %state);
}

function MiniGameSO::addMember(%this, %client)
{
	%client.resetGhosting();

	Parent::addMember(%this, %client);

	%partition = %this.partition;
	if (%partition)
		%client.partition = %partition;

	%client.preActivateGhosting();
	%client.activateGhosting();
	%client.onActivateGhosting();

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

		dsChallengeManagerSO.cancelChallengesTo(%client);
	}
	else if (%this.duel && %this.duelist1 != %client && %this.duelist2 != %client)
	{
		// Trigger spectator mode on player spawn attempt.
		dsChallengeManagerSO.broadcastPlayerUpdate(%client, 5, %client);
		%client.InstantRespawn();
	}
}

function ShapeBase::moveToAndLookAt(%this, %position, %target)
{
	%vec = VectorNormalize(VectorSub(%position, %target));
	%x = getWord(%vec, 0);
	%y = getWord(%vec, 1);
	%z = getWord(%vec, 2);
	%yaw = mAtan(%x, -%y);
	%pitch = mAsin(-%z);
	%rot = getWords(MatrixCreateFromEuler(%pitch SPC 0 SPC %yaw), 3, 6);
	%this.setTransform(%position SPC %rot);
}

function dsWorldPartitionSO::moveCameraToDefaultPosition(%this, %camera)
{
	// TODO: Place the spectator camera in a place that can see the target position and isn't obstructed.
	// TODO: Point to center point of bounding box for spawns.

	%partitionSize = dsWorldPartitionManagerSO.partitionSize;
	%off = %partitionSize / 4;
	%offZ = %off / mSqrt(2);

	%target = %this.position;
	%position = VectorAdd(-%off SPC -%off SPC %offZ, %target);

	%camera.moveToAndLookAt(%position, %target);
}

function dsWorldPartitionSO::loadEnvironment(%this, %map)
{
	%this.pushEnvironment();

	if (%map.environment)
	{
		%groundPlane = %this.groundPlane;
		%sky = %this.sky;
		%sun = %this.sun;
		%sunLight = %this.sunLight;

		%groundPlane.color = %map.groundPlaneColor;
		%groundPlane.blend = getWord(%groundPlane.color, 3) < 255;
		%groundPlane.scrollSpeed = %map.groundPlaneScrollSpeed;
		%groundPlane.sendUpdate();

		%sky.skyColor = %map.skyColor;
		%sky.fogColor = %map.skyFogColor;
		%sky.fogDistance = %map.skyFogDistance;
		%sky.visibleDistance = %map.skyVisibleDistance;
		%sky.sendUpdate();

		%sun.ambient = %map.sunAmbientColor;
		%sun.azimuth = %map.sunAzimuth;
		%sun.color = %map.sunDirectColor;
		%sun.elevation = %map.sunElevation;
		%sun.shadowColor = %map.sunShadowColor;
		%sun.sendUpdate();

		%sunLight.color = %map.sunLightColor;
		%sunLight.FlareSize = %map.sunLightSize;
		%sunLight.setFlareBitmaps(%map.sunLightLocalPath, %map.sunLightRemotePath);
		%sunLight.sendUpdate();
	}
	else
	{
		EnvGuiServer::SetSimpleMode();
		EnvGuiServer::readAdvancedVarsFromSimple();
		EnvGuiServer::SetAdvancedMode();
	}

	%this.popEnvironment();
}

function dsWorldPartitionSO::setEnvGuiPrefs(%this)
{
	%groundPlane = %this.groundPlane;
	%sky = %this.sky;
	%sun = %this.sun;
	%sunLight = %this.sunLight;

	for (%i = $EnvGuiServer::SunFlareCount - 1; %i >= 0; %i--)
		if (%sunLight.LocalFlareBitmap $= $EnvGuiServer::SunFlare[%i])
			break;
	$EnvGuiServer::SunFlareTopIdx = %i;

	for (%i = $EnvGuiServer::SunFlareCount - 1; %i >= 0; %i--)
		if (%sunLight.RemoteFlareBitmap $= $EnvGuiServer::SunFlare[%i])
			break;
	$EnvGuiServer::SunFlareBottomIdx = %i;

	$EnvGuiServer::AmbientLightColor = %sun.ambient;
	$EnvGuiServer::DirectLightColor = %sun.color;
	$EnvGuiServer::FogColor = getColorI(%sky.fogColor);
	$EnvGuiServer::FogDistance = %sky.fogDistance;
	$EnvGuiServer::GroundColor = getColorF(%groundPlane.color);
	$EnvGuiServer::GroundScrollX = getWord(%groundPlane.scrollSpeed, 0);
	$EnvGuiServer::GroundScrollY = getWord(%groundPlane.scrollSpeed, 1);
	$EnvGuiServer::ShadowColor = %sun.shadowColor;
	$EnvGuiServer::SkyColor = getColorI(%sky.skyColor);
	$EnvGuiServer::SunAzimuth = %sun.azimuth;
	$EnvGuiServer::SunElevation = %sun.elevation;
	$EnvGuiServer::SunFlareColor = %sunLight.color;
	$EnvGuiServer::SunFlareSize = %sunLight.FlareSize;
	$EnvGuiServer::VisibleDistance = %sky.visibleDistance;
}

function dsWorldPartitionSO::saveEnvironment(%this, %map)
{
	%groundPlane = %this.groundPlane;
	%sky = %this.sky;
	%sun = %this.sun;
	%sunLight = %this.sunLight;
	%mgr = nameToID(dsWorldPartitionManagerSO);

	if (%mgr.defaultGroundPlaneColor $= %groundPlane.color &&
		%mgr.defaultGroundPlaneScrollSpeed $= %groundPlane.scrollSpeed &&
		%mgr.defaultSkyColor $= %sky.skyColor &&
		%mgr.defaultSkyFogColor $= %sky.fogColor &&
		%mgr.defaultSkyFogDistance == %sky.fogDistance &&
		%mgr.defaultSkyVisibleDistance == %sky.visibleDistance &&
		%mgr.defaultSunAmbientColor $= %sun.ambient &&
		%mgr.defaultSunAzimuth == %sun.azimuth &&
		%mgr.defaultSunDirectColor $= %sun.color &&
		%mgr.defaultSunElevation == %sun.elevation &&
		%mgr.defaultSunShadowColor $= %sun.shadowColor &&
		%mgr.defaultSunLightColor $= %sunLight.color &&
		%mgr.defaultSunLightLocalPath $= %sunLight.LocalFlareBitmap &&
		%mgr.defaultSunLightRemotePath $= %sunLight.RemoteFlareBitmap &&
		%mgr.defaultSunLightSize == %sunLight.FlareSize
	)
	{
		%map.environment = "";

		%map.groundPlaneColor = "";
		%map.groundPlaneScrollSpeed = "";

		%map.skyColor = "";
		%map.skyFogColor = "";
		%map.skyFogDistance = "";
		%map.skyVisibleDistance = "";

		%map.sunAmbientColor = "";
		%map.sunAzimuth = "";
		%map.sunDirectColor = "";
		%map.sunElevation = "";
		%map.sunShadowColor = "";

		%map.sunLightColor = "";
		%map.sunLightLocalPath = "";
		%map.sunLightRemotePath = "";
		%map.sunLightSize = "";

		return;
	}

	%map.environment = 1;

	%map.groundPlaneColor = %groundPlane.color;
	%map.groundPlaneScrollSpeed = %groundPlane.scrollSpeed;

	%map.skyColor = %sky.skyColor;
	%map.skyFogColor = %sky.fogColor;
	%map.skyFogDistance = %sky.fogDistance;
	%map.skyVisibleDistance = %sky.visibleDistance;

	%map.sunAmbientColor = %sun.ambient;
	%map.sunAzimuth = %sun.azimuth;
	%map.sunDirectColor = %sun.color;
	%map.sunElevation = %sun.elevation;
	%map.sunShadowColor = %sun.shadowColor;

	%map.sunLightColor = %sunLight.color;
	%map.sunLightLocalPath = %sunLight.LocalFlareBitmap;
	%map.sunLightRemotePath = %sunLight.RemoteFlareBitmap;
	%map.sunLightSize = %sunLight.FlareSize;
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
				%this.lastResetTime = 0;
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
	%owner = %this.owner;

	if (%this.buildSession)
	{
		%this.commandToAll('dcSetBuilding', 0);
		%partition = %this.partition;

		if (isFile(%path = dsMapManagerSO.directory @ %owner.bl_id @ "/.backup.cs"))
			fileDelete(%path);

		%path = dsMapManagerSO.directory @ %owner.bl_id @ "/.backup.bls";
		if (%partition.bricks.getCount())
			%partition.queueSaveBricks(%path);
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

	%owner.InstantRespawn();
	if (%this.buildSession && !%owner.isAdmin)
		commandToClient(%owner, 'SetAdminLevel', 0);

	for (%i = 0; %i < %numMembers; %i++)
	{
		%member = %member[%i];

		%member.partition = dsWorldPartitionManagerSO.centerPartition;
		%member.preActivateGhosting();
		%member.activateGhosting();
		%member.onActivateGhosting();
		dsChallengeManagerSO.broadcastPlayerUpdate(%member, %member.dcClient ? 1 : 0, %member);
	}

	%partition = %this.partition;
	if (isObject(%partition))
	{
		%partition.miniGame = "";
		dsWorldPartitionManagerSO.queueRelease(%partition);
		%this.partition = "";
	}
}

function dsWorldPartitionSO::cycleGhosting(%this)
{
	if (!isObject(%miniGame = %this.miniGame))
		return;

	%numMembers = %miniGame.numMembers;
	for (%i = 0; %i < %numMembers; %i++)
	{
		%member = %miniGame.member[%i];
		%member.resetGhosting();
		%member.preActivateGhosting();
		%member.activateGhosting();
		%member.onActivateGhosting();
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
		%client.preActivateGhosting();
		%client.activateGhosting();
		%client.onActivateGhosting();
	}
}

function MiniGameSO::startDuel(%this, %client)
{
	%this.lastResetTime = 0;
	%this.reset(%client);
	%this.duelStarted = 1;
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

function GameConnection::InstantRespawn(%this)
{
	// Prevent spectators from instant respawning on reset.
	if (isObject(%miniGame = %this.miniGame))
	{
		// Time values for comparison are not reliable w/o forcing to ints.
		%currentTime = getSimTime() | 0;
		%lastResetTime = %miniGame.lastResetTime | 0;

		if (%miniGame.duel && %miniGame.duelStarted &&
			(%lastResetTime == %currentTime || %lastResetTime == 0) &&
			isObject(%this.player) && %this.player.getDamagePercent() < 1 &&
			%this != %miniGame.duelist1 && %this != %miniGame.duelist2)
		{
			return;
		}
	}

	Parent::InstantRespawn(%this);
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

	%this.updateEnableBuilding();
	%this.updateEnablePainting();
	%this.duelReady = 1;
	%this.duelEnded = "";
	%this.startDuel(%client);

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
	%this.duelStarted = "";
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

	%this.updateEnableBuilding();
	%this.updateEnablePainting();

	%this.lastResetTime = 0;
	%this.reset(%client);

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
	{
		if (%this.duel && %client != %this.duelist1 && %client != %this.duelist2)
			return %partition.pickSpectatorSpawnPoint();
		else
			return %partition.pickSpawnPoint();
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
	if (%this.hasSpawnedOnce)
	{
		// This is necessary to prevent resetting the client state after a call to GameConnection::activateGhosting.
		return;
	}

	Parent::onGhostAlwaysObjectsReceived(%this);
}

function GameConnection::preActivateGhosting(%this)
{
	if ((%partition = %this.partition) && (%colorSet = %partition.colorSet))
	{
		%colorSet.apply();
		%this.transmitStaticBrickData();

		%miniGame = %this.miniGame;
		if (!isObject(%miniGame) || %miniGame.buildSession)
			commandToClient(%this, 'PlayGui_LoadPaint');
	}
}

function GameConnection::onActivateGhosting(%this)
{
	%camera = %this.camera;
	%player = %this.player;

	if (isObject(%camera))
		%camera.scopeToClient(%this);
	if (isObject(%player))
		%player.scopeToClient(%this);

	if (%partition = %this.partition)
		%partition.scopeEnvironment(%this);
}

function serverCmdEnvGui_RequestCurrentVars(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client)
	{
		%backupAdmin = %client.isAdmin;
		%client.isAdmin = 1;
		%override = 1;
	}

	%client.partition.setEnvGuiPrefs();

	Parent::serverCmdEnvGui_RequestCurrentVars(%client);

	if (%override)
		%client.isAdmin = %backupAdmin;
}

function serverCmdEnvGui_RequestLists(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client)
	{
		%backupAdmin = %client.isAdmin;
		%client.isAdmin = 1;
		%override = 1;
	}

	%client.partition.setEnvGuiPrefs();

	Parent::serverCmdEnvGui_RequestLists(%client);

	if (%override)
		%client.isAdmin = %backupAdmin;
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
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client && !%miniGame.testDuel)
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
	dsWorldPartitionManagerSO.centerPartition.queueClearBricks();
}

function dsSaveCenter()
{
	if (isFile(%path = dsMapManagerSO.directory @ ".center.cs"))
		fileDelete(%path);

	%path = dsMapManagerSO.directory @ ".center.bls";
	%partition = dsWorldPartitionManagerSO.centerPartition;
	%partition.queueSaveBricks(%path, 0, 0);
}

function dsLoadCenter()
{
	if (isFile(%path = dsMapManagerSO.directory @ ".center.bls"))
		dsWorldPartitionManagerSO.centerPartition.queueLoadBricks(%path);
}

function serverCmdLight(%client)
{
	// Prevent light use on spawn to account for ghosting tricks.
	// When a client spawns with ghosting turned of, they think the map is dark regardless of whether it actually is, and the auto-light kicks in.
	%player = %client.player;
	if (isObject(%player) && (getSimTime() - %player.lightCheckSpawnTime) < 1000)
		return;

	Parent::serverCmdLight(%client);
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

function serverCmdEnvGui_ClickDefaults(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client)
	{
		%backupAdmin = %client.isAdmin;
		%client.isAdmin = 1;
		%override = 1;
	}

	if (!%client.isAdmin)
		return;

	%partition = %client.partition;

	if (%partition)
		%partition.pushEnvironment();

	Parent::serverCmdEnvGui_ClickDefaults(%client);
	Sky.visibleDistance = dsWorldPartitionManagerSO.defaultSkyVisibleDistance;

	if (%partition)
		%partition.popEnvironment();

	if (%override)
		%client.isAdmin = %backupAdmin;
}

function serverCmdEnvGui_SetVar(%client, %name, %value)
{
	if (strstr($dsBannedEnvGuiVars, " " @ %name @ " ") != -1)
		return;

	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.buildSession && %miniGame.owner == %client)
	{
		%backupAdmin = %client.isAdmin;
		%client.isAdmin = 1;
		%override = 1;
	}

	if (!%client.isAdmin)
		return;

	%partition = %client.partition;

	if (%partition)
		%partition.pushEnvironment();

	Parent::serverCmdEnvGui_SetVar(%client, %name, %value);

	if (%partition)
		%partition.popEnvironment();

	if (%override)
		%client.isAdmin = %backupAdmin;
}

function serverCmdMissionStartPhase2Ack(%client, %a)
{
	if (!%client.hasSpawnedOnce)
	{
		%colorSet = dsWorldPartitionManagerSO.centerPartition.colorSet;
		if (%colorSet)
			%colorSet.apply();
	}

	Parent::serverCmdMissionStartPhase2Ack(%client, %a);
}

function Projectile::onAdd(%this)
{
	Parent::onAdd(%this);

	if (isObject(%client = %this.client))
	{
		if (isObject(%miniGame = %client.miniGame) && %miniGame.duel)
		{
			%this.scopeToClient(%miniGame.duelist1);
			%this.scopeToClient(%miniGame.duelist2);
		}
		else
		{
			%this.scopeToClient(%client);
		}
	}
}

function SimObject::setNTObjectName(%this, %name)
{
	if (isObject(%partition = %this.partition))
	{
		echo(%partition SPC %name);
	}

	Parent::setNTObjectName(%this, %name);
}

}; // package Server_Dueling

// TODO: Implement vignette by overriding EnvGuiServer::SendVignetteAll.
$dsBannedEnvGuiVars = " DayCycleIdx DayCycleEnabled DayLength DayOffset GroundIdx SimpleMode SkyIdx UnderWaterColor VignetteColor VignetteMultiply WaterColor WaterHeight WaterIdx WaterScrollX WaterScrollY ";

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

function miniGameCanUse(%a, %b)
{
	// If the player is in the vacinity of something, we can assume it's fine for them to use it.
	return 1;
}

}; // package Server_Dueling_Deferred
