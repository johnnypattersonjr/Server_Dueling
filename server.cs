// Copyright (c) Johnny Patterson

exec("./scripts/bricks.cs");
exec("./scripts/minimalDefaultContent.cs");

exec("./scripts/dsChallengeManagerSO.cs");
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
		// position = "100000 100000 100000";
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

function startGame()
{
	%boundarySize = 64;
	%arenaSize = 256;
	%partitionSize = %arenaSize + %boundarySize;
	%partitionSeparation = %partitionSize * 2;

	DisabledDataBlockGroup();

	dsChallengeManagerSO();
	dsCreateGhostingSky();
	dsStatManagerSO();
	dsWeaponManagerSO().buildListFromDatablockGroup();

	dsSetGhostingDistance(%partitionSize);
	Sky.visibleDistance = %partitionSize * 2;
	dsWorldPartitionManagerSO().generate(%partitionSeparation, %partitionSize, 256);

	dsLegacyDataServer($Server::Port + 7);
	Parent::startGame();
}

}; // package Server_Dueling

activatePackage(Server_Dueling);
