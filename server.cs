// Copyright (c) Johnny Patterson

exec("./scripts/bricks.cs");
exec("./scripts/minimalDefaultContent.cs");

exec("./scripts/dsChallengeManagerSO.cs");
exec("./scripts/dsColorSetManagerSO.cs");
exec("./scripts/dsMapManagerSO.cs");
exec("./scripts/dsStatManagerSO.cs");
exec("./scripts/dsWeaponManagerSO.cs");
exec("./scripts/dsWorldPartitionManagerSO.cs");

exec("./scripts/legacy.cs");

package Server_Dueling {

function MiniGameSO::commandToAll(%this, %cmd, %arg1, %arg2, %arg3, %arg4, %arg5, %arg6, %arg7, %arg8, %arg9, %arg10)
{
	%count = %this.numMembers;
	for (%i = 0; %i < %count; %i++)
	{
		%obj = %this.member[%i];
		if (%obj.isAIControlled())
			continue;

		commandToClient(%obj, %cmd, %arg1, %arg2, %arg3, %arg4, %arg5, %arg6, %arg7, %arg8, %arg9, %arg10);
	}
}

function MiniGameSO::commandToAllExcept(%this, %client, %cmd, %arg1, %arg2, %arg3, %arg4, %arg5, %arg6, %arg7, %arg8, %arg9, %arg10)
{
	%count = %this.numMembers;
	for (%i = 0; %i < %count; %i++)
	{
		%obj = %this.member[%i];
		if (%obj.isAIControlled() || %obj == %client)
			continue;

		commandToClient(%obj, %cmd, %arg1, %arg2, %arg3, %arg4, %arg5, %arg6, %arg7, %arg8, %arg9, %arg10);
	}
}

function commandToAllExcept(%client, %cmd, %arg1, %arg2, %arg3, %arg4, %arg5, %arg6, %arg7, %arg8, %arg9, %arg10)
{
	%group = nameToID(ClientGroup);
	%count = %group.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%obj = %group.getObject(%i);
		if (%obj.isAIControlled() || %obj == %client)
			continue;

		commandToClient(%obj, %cmd, %arg1, %arg2, %arg3, %arg4, %arg5, %arg6, %arg7, %arg8, %arg9, %arg10);
	}
}

function dsCreateGhostingSky()
{
	// The server-side camera query used for ghosting uses the last created Sky.
	%sky = new Sky(:Sky)
	{
		position = "0 0 0";
		scale = "0 0 0";
		visibleDistance = 0; // Has internal minimum of 50.
	};

	// Skies will be force named "Sky" on create, so names must be set after.
	%sky.setName("GhostingSky");

	%sky.setScopeAlways();
	GhostAlwaysSet.remove(%sky);
}

function dsExec()
{
	exec("Add-Ons/Server_Dueling/server.cs");
}

function dsSetGhostingDistance(%distance)
{
	GhostingSky.visibleDistance = %distance;
}

function dsStartGame()
{
	stopRaytracer();

	dsColorSetManagerSO().makeSprayCanGeneric();

	%boundarySize = 64;
	%arenaSize = 256;
	%partitionSize = %arenaSize + %boundarySize;
	%partitionSeparation = %partitionSize * 2;

	DisabledDataBlockGroup();

	Sky.visibleDistance = %partitionSize * 2;
	dsWorldPartitionManagerSO().generate(%partitionSeparation, %partitionSize, 128);

	dsChallengeManagerSO();
	dsCreateGhostingSky();
	dsStatManagerSO();
	dsWeaponManagerSO().buildListFromDatablockGroup();

	dsSetGhostingDistance(%partitionSize);

	dsMapManagerSO();

	dsLegacyDataServer($Server::Port + 7);
}

function startGame()
{
	Parent::startGame();

	// This is deferred so there is time for the environment to init.
	schedule(0, 0, dsStartGame);
}

function detectHang(%a)
{
	cancel($detectHangEvent);
	if (!%a)
		return;

	%realTime = getRealTime();

	if ($detectHangLastRealTime !$= "")
	{
		%threshold = 3 * 1000 / 32;

		%deltaRealTime = %realTime - $detectHangLastRealTime;
		if (%deltaRealTime > %threshold)
		{
			%msg = "Hang: " @ %deltaRealTime @ " ms";
			talk(%msg);
			echo(%msg);
		}
	}

	$detectHangLastRealTime = %realTime;

	$detectHangEvent = schedule(0, 0, detectHang, %a);
}

function serverCmdDetectHangs(%client, %a)
{
	if (!%client.isAdmin)
		return;

	detectHang(%a);
}

}; // package Server_Dueling

activatePackage(Server_Dueling);

// Use a separate deferred package to override possible undesired behavior from other mods.
schedule(1000, 0, activatePackage, Server_Dueling_Deferred);
