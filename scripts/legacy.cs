// Copyright (c) Johnny Patterson

// commandToClient(%client, 'dcRemoveMap', %id);

package Server_Dueling {

function serverCmddcAccept(%client, %id)
{
	if (%client.challenging)
		return;

	%miniGame = %client.miniGame;

	if (isObject(%miniGame) && (!%miniGame.buildSession || (%miniGame.duel && !%miniGame.testDuel)))
		return;

	%result = dsChallengeManagerSO.accept(%client, %id);

	if (%result == -1)
		commandToClient(%client, 'MessageBoxOK', 'Dueling Error', 'Could not allocate world partition.');
}

function serverCmddcAlertMsgConfirm(%client, %map)
{
	if (!isObject(%map) || %map.submitterID != %client.bl_id || %map.state != -1)
		return;

	%map.feedback = "";
	%map.state = 0;
	%map.saveMap();

	commandToClient(%client, 'dcSetSave', %map, %map.name, 0, 0, 0);
}

function serverCmddcAlertMsgRequest(%client, %map)
{
	if (!isObject(%map) || %map.submitterID != %client.bl_id || %map.state != -1)
		return;

	commandToClient(%client, 'dcAlertMsg', %map.feedback);
}

function serverCmddcBoast(%client, %weaponName, %goal)
{
	if (%client.challenging)
		return;

	%goal = mFloatLength(%goal, 0);
	if (%goal < dsChallengeManagerSO.minGoal || %goal > dsChallengeManagerSO.maxGoal)
		return;

	%weapon = dsWeaponManagerSO().findWeaponByUIName(%weaponName);
	if (%weapon < 0)
		return;

	dsChallengeManagerSO.addBoast(%client, %weapon, %goal);
}

function serverCmddcCancel(%client)
{
	if (!%client.challenging)
		return;

	dsChallengeManagerSO.cancelChallenge(%client);
}

function serverCmddcCenterBricks(%client)
{
	%miniGame = %client.miniGame;
	if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
		return;

	if (%miniGame.partition.centerBricks())
		%miniGame.messageAll('', "\c3" @ %client.name @ " \c2centered the bricks!");
}

function serverCmddcChallenge(%client, %target, %weaponName, %goal)
{
	%goal = mFloatLength(%goal, 0);
	%target = mFloatLength(%target, 0);

	if (!isObject(%target) || nameToID(%target.getGroup()) != nameToID(ClientGroup) || %client.challenging || %goal < dsChallengeManagerSO.minGoal || %goal > dsChallengeManagerSO.maxGoal)
		return;

	%weapon = dsWeaponManagerSO().findWeaponByUIName(%weaponName);
	if (%weapon < 0)
		return;

	dsChallengeManagerSO.addChallenge(%client, %target, %weapon, %goal);
	messageClient(%client, '', "\c2You challenged \c3" @ %target.name @ " \c2to \c4" @ %weapon.uiName @ "\c2, first to \c4" @ %goal @ "\c2!");
	messageClient(%target, '', "\c3" @ %client.name @ " \c2challenged you to \c4" @ %weapon.uiName @ "\c2, first to \c4" @ %goal @ "\c2!");
}

function serverCmddcClearBricks(%client)
{
	%miniGame = %client.miniGame;
	if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
		return;

	if (%miniGame.partition.clearBricks())
		%miniGame.messageAll('', "\c3" @ %client.name @ " \c2cleared the bricks!");
}

function serverCmddcCloseDuelPane(%client)
{
	dsChallengeManagerSO.duelPaneList.remove(%client);
}

function serverCmddcDecline(%client, %source)
{
	if (!isObject(%client.challengeList))
		return;

	%source = mFloatLength(%source, 0) * -1;

	dsChallengeManagerSO.removeChallenge(%source, %client);
	messageClient(%client, '', "\c3" @ %source.name @ " \c2rejected your challenge!");
}

function serverCmddcDeleteSave(%client, %map)
{
	if (!isObject(%map) || %map.submitterID != %client.bl_id)
	{
		commandToClient(%client, 'MessageBoxOK', 'Delete Error', 'Could not find save.');
		return;
	}

	%name = %map.name;
	%directory = dsMapManagerSO.directory @ %map.submitterID @ "/";

	%path = %directory @ %name @ ".cs";
	if (isFile(%path))
		fileDelete(%path);

	%path = %directory @ %name @ "-bricks.cs";
	if (isFile(%path))
		fileDelete(%path);

	%map.delete();
	commandToClient(%client, 'dcRemoveSave', %map);
	messageClient(%client, '', "\c2You deleted \c3\"" @ %name @ "\"\c2!");
}

function serverCmddcEditInfo(%client, %map)
{
	if (!isObject(%map) || %map.submitterID != %client.bl_id || %map.state != 1)
		return;

	%owners = %map.owners;
	%count = getFieldCount(%owners);

	if (%count)
	{
		%builders = getWord(getField(%owners, 0), 0);

		for (%i = 1; %i < %count; %i++)
			%builders = %builders TAB getWord(getField(%owners, %i), 0);
	}

	%weapons = dsWeaponManagerSO.list;
	%count = getFieldCount(%weapons);

	for (%i = 0; %i < %count; %i++)
	{
		%weapon = getField(%weapons, %i);
		%itemName = %weapon.getName();

		%targetState = setField(%targetState, %i, %map.target[%itemName] ? 1 : 0);
	}

	%client.infoRequestMap = %map;
	commandToClient(%client, 'dcRequestInfo', %map, %targetState, %builders);
}

function serverCmddcLoadBackup(%client, %yes)
{
	// NOTE: Backups are auto-loaded for now, so people without the client can build without losing progress.

	// %miniGame = %client.miniGame;
	// if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
	// 	return;

	// %path = dsMapManagerSO.directory @ %client.bl_id @ "/.backup.cs";
	// if (!isFile(%path))
	// 	return;

	// if (%yes)
	// 	%miniGame.partition.loadBricks(%path);

	// fileDelete(%path);
}

function serverCmddcLoadMap(%client, %map)
{
	%miniGame = %client.miniGame;
	if (!%client.isAdmin || !isObject(%map) || %map.state != 2 || !isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
		return;

	if (!isObject(%map))
	{
		commandToClient(%client, 'MessageBoxOK', 'Load Error', "Could not find map.");
		return;
	}

	%weapons = dsWeaponManagerSO.list;
	%count = getFieldCount(%weapons);

	for (%i = 0; %i < %count; %i++)
	{
		%weapon = getField(%weapons, %i);
		%itemName = %weapon.getName();

		if (%map.target[%itemName])
		{
			if (%numWeapons++ == 1)
				%targetString = %weapon.uiName;
			else
				%targetString = %targetString @ ", " @ %weapon.uiName;
		}
	}

	if (%miniGame.partition.clearBricks())
		%miniGame.messageAll('', "\c3" @ %client.name @ " \c2cleared the bricks!");

	%name = %map.name;
	%directory = dsMapManagerSO.directory @ %map.submitterID @ "/";
	%path = %directory @ %name @ "-bricks.cs";
	%partition = %client.partition;
	%partition.loadEnvironment(%map);
	%partition.loadBricks(%path);
	%miniGame.messageAll('', "\c3" @ %client.name @ " \c2loaded map \c3\"" @ %name @ "\"\c2!");
	%miniGame.messageAll('', "\c4Builders: \c3" @ %map.getOwnersPrettyString());
	%miniGame.messageAll('', "\c4Weapons: \c3" @ %targetString);
	%miniGame.Reset(%client);
}

function serverCmddcLoadSave(%client, %map)
{
	%miniGame = %client.miniGame;
	if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
		return;

	if (!isObject(%map) || %map.submitterID != %client.bl_id)
	{
		commandToClient(%client, 'MessageBoxOK', 'Load Error', "Could not find save \"" @ %name @ "\".");
		return;
	}

	%name = %map.name;
	%directory = dsMapManagerSO.directory @ %map.submitterID @ "/";
	%path = %directory @ %name @ "-bricks.cs";
	%partition = %client.partition;
	%partition.loadEnvironment(%map);
	%partition.loadBricks(%path);
	%miniGame.messageAll('', "\c3" @ %client.name @ " \c2loaded \c3\"" @ %name @ "\"\c2!");
	%miniGame.messageAll('', "\c4Builders: \c3" @ %map.getOwnersPrettyString());
}

function serverCmddcLoadSubmission(%client, %map)
{
	%miniGame = %client.miniGame;
	if (!%client.isAdmin || !isObject(%map) || %map.state != 1 || !isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
		return;

	if (!isObject(%map))
	{
		commandToClient(%client, 'MessageBoxOK', 'Load Error', "Could not find submission.");
		return;
	}

	%weapons = dsWeaponManagerSO.list;
	%count = getFieldCount(%weapons);

	for (%i = 0; %i < %count; %i++)
	{
		%weapon = getField(%weapons, %i);
		%itemName = %weapon.getName();

		if (%map.target[%itemName])
		{
			if (%numWeapons++ == 1)
				%targetString = %weapon.uiName;
			else
				%targetString = %targetString @ ", " @ %weapon.uiName;
		}
	}

	if (%miniGame.partition.clearBricks())
		%miniGame.messageAll('', "\c3" @ %client.name @ " \c2cleared the bricks!");

	%name = %map.name;
	%directory = dsMapManagerSO.directory @ %map.submitterID @ "/";
	%path = %directory @ %name @ "-bricks.cs";
	%partition = %client.partition;
	%partition.loadEnvironment(%map);
	%partition.loadBricks(%path);
	%miniGame.messageAll('', "\c3" @ %client.name @ " \c2loaded submission \c3\"" @ %name @ "\"\c2!");
	%miniGame.messageAll('', "\c4Builders: \c3" @ %map.getOwnersPrettyString());
	%miniGame.messageAll('', "\c4Weapons: \c3" @ %targetString);
	%miniGame.Reset(%client);
}

function serverCmddcMapRating(%client, %rating)
{
	%map = %client.lastMap;
	%target = %client.lastTarget;

	if (!%map || !%target)
		return;

	if (%rating > 0)
	{
		%map.goodRatings[%target.getName()]++;
		%map.saveMap();
	}
	else if (%rating < 0)
	{
		%map.badRatings[%target.getName()]++;
		%map.saveMap();
	}

	%client.lastMap = "";
	%client.lastTarget = "";
}

function serverCmddcOpenDuelPane(%client)
{
	%group = nameToID(ClientGroup);
	%count = %group.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%obj = %group.getObject(%i);
		if (%obj == %client)
			continue;

		%status = %obj.dcClient ? 1 : 0;
		%miniGame = %obj.miniGame;

		if (isObject(%miniGame))
		{
			if (%miniGame.buildSession)
				%status = 2; // Building
			else if (%obj == %miniGame.duelist1 || %obj == %miniGame.duelist2)
				%status = 4; // Dueling
			else
				%status = 5; // Spectating
		}
		else if (%obj.dcClient)
		{
			%status = 1; // Available
		}
		else
		{
			%status = 0; // Not Challengable
		}

		commandToClient(%client, 'dcSetPlayer', %obj, %obj.name, %status);
	}

	dsChallengeManagerSO.duelPaneList.add(%client);
}

function serverCmddcOverwrite(%client)
{
	%miniGame = %client.miniGame;
	if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
		return;

	%name = %client.saveAttemptName;
	%client.saveAttemptName = "";

	%map = dsMapManagerSO.findSave(%client, %name);
	if (!isObject(%map))
	{
		// This shouldn't happen normally, but it can if the client or admin deletes a map during an overwrite prompts.
		commandToClient(%client, 'MessageBoxOK', 'Save Error', "Could not find save \"" @ %name @ "\".");
		return;
	}

	%name = %map.name;
	%directory = dsMapManagerSO.directory @ %map.submitterID @ "/";
	%path = %directory @ %name @ "-bricks.cs";
	%partition = %client.partition;

	if (%partition.saveBricks(%path, 1, 1))
	{
		%map.owners = %partition.saveOwners;
		%map.worldBox = %partition.saveWorldBox;
		%partition.saveEnvironment(%map);
		%map.saveMap();

		%miniGame.messageAll('', "\c3" @ %client.name @ " \c2saved the bricks as \c3\"" @ %name @ "\"\c2!");
		%miniGame.messageAll('', "\c4Builders: \c3" @ %map.getOwnersPrettyString());
	}
	else
	{
		%miniGame.messageAll('', "\c0No bricks found!");
	}
}

function serverCmddcPong(%client, %version, %revision)
{
	%client.dcClient = 1;

	if (%version < 2 || (%version == 2 && %revision == 0))
		dsChallengeManagerSO.duelPaneList.add(%client);

	dsChallengeManagerSO.broadcastPlayerUpdate(%client, 1, %client);
	serverCmddcRequestTransmission(%client);
}

function serverCmddcRequestTransmission(%client)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame))
	{
		%building = %miniGame.buildSession;
		%dueling = !%building && %miniGame.duel && (%miniGame.duelist1 == %client || %miniGame.duelist2 == %client);

		if (%building && %miniGame.owner == %client)
			%building = 2;
	}
	else
	{
		%building = 0;
		%dueling = 0;
	}

	commandToClient(%client, 'dcSetBuilding', %building);
	commandToClient(%client, 'dcSetDueling', %dueling);
	commandToClient(%client, 'dcSetMode', 1);
	commandToClient(%client, 'dcSetWeapons', dsWeaponManagerSO().list);

	%list = dsChallengeManagerSO.boastList;
	%count = %list.rowCount();

	for (%i = 0; %i < %count; %i++)
	{
		%source = %list.getRowId(%i);
		if (%source == %client)
			continue;

		%row = %list.getRowText(%i);

		commandToClient(%client, 'dcSetChallenge', %source, getField(%row, 0), getField(%row, 1), 0);
	}

	%list = %client.challengeList;
	if (isObject(%list))
	{
		%count = %list.rowCount();

		for (%i = 0; %i < %count; %i++)
		{
			%source = %list.getRowId(%i);
			%row = %list.getRowText(%i);

			commandToClient(%client, 'dcSetChallenge', -1 * %source, getField(%row, 0), getField(%row, 1), 1, %source.name);
		}
	}

	if (%client.challenging)
	{
		%target = %client.challenging == 1 ? %client.challengeTarget.name : "";
		commandToClient(%client, 'dcSetChallenging', 1, getField(%client.challengeInfo, 0), getField(%client.challengeInfo, 1), %client.challenging == 2 ? 0 : 1, %target);
	}

	if (%building)
	{
		%partition = %miniGame.partition;
		commandToClient(%client, 'dcSetBrickCount', %partition.bricks.getCount(), %partition.maxBricks);
	}

	if (%building == 2)
	{
		%count = %miniGame.numMembers;

		for (%i = 0; %i < %count; %i++)
		{
			%member = %miniGame.member[%i];
			commandToClient(%client, 'dcSetBuildSessionMember', %member, %member.name);
		}
	}

	if (%record = %client.statRecord)
	{
		if (%maps = %record.maps)
		{
			%count = %maps.getCount();

			for (%i = 0; %i < %count; %i++)
			{
				%map = %maps.getObject(%i);
				commandToClient(%client, 'dcSetSave', %map, %map.name, %map.state | 0, 0, 0);
			}
		}
	}

	if (%client.isAdmin)
		dsMapManagerSO.sendSubmissions(%client);
}

function serverCmddcReviewSubmission(%client, %map, %accept, %feedback)
{
	if (!%client.isAdmin || !isObject(%map) || %map.state != 1)
		return;

	dsMapManagerSO.removeSubmission(%map);

	if (%accept)
	{
		%weapons = dsWeaponManagerSO.list;
		%count = getFieldCount(%weapons);

		for (%i = 0; %i < %count; %i++)
		{
			%weapon = getField(%weapons, %i);
			%itemName = %weapon.getName();

			if (%map.target[%itemName])
				%weapon.maps.add(%map);
		}

		%map.state = 2;
	}
	else
	{
		%map.state = -1;
		%map.feedback = %feedback;
	}

	%map.reviewerID = %client.bl_id;
	%map.reviewerName = %client.name;
	%map.saveMap();

	if (isObject(%submitter = findClientByBL_ID(%map.submitterID)))
		commandToClient(%submitter, 'dcSetSave', %map, %map.name, %map.state | 0, 0, 0);
}

function serverCmddcRevokeMap(%client, %map, %feedback)
{
	if (!%client.isAdmin || !isObject(%map) || %map.state != 2)
		return;

	%weapons = dsWeaponManagerSO.list;
	%count = getFieldCount(%weapons);

	for (%i = 0; %i < %count; %i++)
	{
		%weapon = getField(%weapons, %i);
		%itemName = %weapon.getName();

		if (%map.target[%itemName])
			%weapon.maps.remove(%map);
	}

	%map.state = -1;
	%map.feedback = %feedback;
	%map.saveMap();

	if (%client.viewMapsTargetIdx !$= "")
		commandToAll('dcRefreshMaps', %client.viewMapsTargetIdx);

	if (isObject(%submitter = findClientByBL_ID(%map.submitterID)))
		commandToClient(%submitter, 'dcSetSave', %map, %map.name, %map.state | 0, 0, 0);
}

function serverCmddcSaveBricks(%client, %name)
{
	%miniGame = %client.miniGame;
	if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client)
		return;

	%len = strlen(%name);
	%brickSuffixPos = strpos(strlwr(%name), "-bricks");

	if (strpos(%name, ".") == 0 || (%brickSuffixPos != -1 && %brickSuffixPos == (%len - 7)))
	{
		commandToClient(%client, 'MessageBoxOK', 'Save Error', "Save name is invalid.");
		return;
	}

	%pathBan = "\\/:*?\"<>|";
	for (%i = 0; %i < %len; %i++)
	{
		%c = getSubStr(%name, %i, 1);

		if (strpos(%pathBan, %c) < 0 && strcmp(%c, "\x1f") > 0 && strcmp(%c, "\x7f") < 0)
			%sanitized = %sanitized @ %c;
	}

	if (%sanitized !$= %name)
	{
		commandToClient(%client, 'MessageBoxOK', 'Save Error', 'Save name is invalid.');
		return;
	}

	%name = "";

	// Remove consecutive spaces
	for (%i = 0; %i < %len; %i++)
	{
		%c = getSubStr(%sanitized, %i, 1);

		if (%c !$= " " || %lastC !$= " ")
			%name = %name @ %c;

		%lastC = %c;
	}

	%name = trim(%name);

	if (%map = dsMapManagerSO.findSave(%client, %name))
	{
		%client.saveAttemptName = %name;
		commandToClient(%client, 'dcOverwrite');
		return;
	}

	%record = %client.statRecord;
	%maxSaves = %record.maxSaves;

	if (!%maxSaves)
		%maxSaves = dsStatManagerSO.defaultMaxSaves;

	if (%record.maps.getCount() >= %maxSaves)
	{
		commandToClient(%client, 'dcSaveLimit');
		return;
	}

	if (%map = dsMapManagerSO.createAndSave(%client, %name))
	{
		commandToClient(%client, 'dcSetSave', %map, %name, 0, 0, 0);
		%miniGame.messageAll('', "\c3" @ %client.name @ " \c2saved the bricks as \c3\"" @ %name @ "\"\c2!");
		%miniGame.messageAll('', "\c4Builders: \c3" @ %map.getOwnersPrettyString());
	}
	else
	{
		%miniGame.messageAll('', "\c0No bricks found!");
	}
}

function serverCmddcSpectate(%client, %id)
{
	if (!isObject(%id))
		return;

	%miniGame = %client.miniGame;

	if (!isObject(%miniGame) || %miniGame.buildSession || (%miniGame.duelist1 != %miniGame && %miniGame.duelist2 != %miniGame))
	{
		%targetMiniGame = %id.miniGame;

		if (isObject(%targetMiniGame) && %targetMiniGame != %miniGame && !%targetMiniGame.buildSession && %targetMiniGame.duel)
			%targetMiniGame.addMember(%client);
	}
}

function serverCmddcStartBuilding(%client)
{
	if (isObject(%client.miniGame))
		return false;

	if ($Pref::Server::Dueling::NoBuilding)
	{
		commandToClient(%client, 'MessageBoxOK', 'Dueling Error', 'Building is disabled.');
		return false;
	}

	%partition = dsWorldPartitionManagerSO.acquire();
	if (!isObject(%partition))
	{
		commandToClient(%client, 'MessageBoxOK', 'Dueling Error', 'Could not allocate world partition.');
		return false;
	}

	%partition.hostBuildingSession(%client);
	return true;
}

function serverCmddcStartTestDuel(%client, %duelists, %weaponName, %goal)
{
	%miniGame = %client.miniGame;

	if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client || %miniGame.testDuel)
		return;

	%goal = mFloatLength(%goal, 0);
	if (%goal < dsChallengeManagerSO.minGoal || %goal > dsChallengeManagerSO.maxGoal)
		return;

	%duelistCount = getWordCount(%duelists);
	if (%duelistCount < 2)
		return;

	%duelist1 = mFloatLength(getWord(%duelists, 0), 0);
	%duelist2 = mFloatLength(getWord(%duelists, 1), 0);
	if (!isObject(%duelist1) || !isObject(%duelist2) || %duelist1.miniGame != %miniGame || %duelist2.miniGame != %miniGame)
		return;

	%weapon = dsWeaponManagerSO().findWeaponByUIName(%weaponName);
	if (%weapon < 0)
		return;

	%miniGame.startTestDuel(%client, %duelist1, %duelist2, %weapon, %goal);
}

function serverCmddcStopBuilding(%client)
{
	if (!isObject(%client.miniGame) || !%client.miniGame.buildSession)
		return;

	%miniGame = %client.miniGame;

	if (%miniGame.owner == %client)
	{
		if (!%miniGame.ending)
			%miniGame.endGame();

		%miniGame.delete();
	}
	else
	{
		%miniGame.removeMember(%client);
	}

	commandToClient(%client, 'dcSetTestDuelStatus', 0);
}

function serverCmddcStopTestDuel(%client)
{
	%miniGame = %client.miniGame;

	if (!isObject(%miniGame) || !%miniGame.buildSession || %miniGame.owner != %client || !%miniGame.testDuel)
		return;

	%miniGame.stopDuel(%client);
}

function serverCmddcSubmit(%client, %map)
{
	if (!isObject(%map) || %map.submitterID != %client.bl_id)
		return;

	%owners = %map.owners;
	%count = getFieldCount(%owners);

	if (%count)
	{
		%builders = getWord(getField(%owners, 0), 0);

		for (%i = 1; %i < %count; %i++)
			%builders = %builders TAB getWord(getField(%owners, %i), 0);
	}

	%client.infoRequestMap = %map;

	commandToClient(%client, 'dcRequestInfo', %map, dsWeaponManagerSO.defaultTargetState, %builders);
}

function serverCmddcSubmitInfo(%client, %targetState, %builders)
{
	%map = %client.infoRequestMap;
	if (!isObject(%map) || %map.submitterID != %client.bl_id)
		return;

	// TODO: Merge owners and builders in a separate list
	// TEMP: Builders list is ignored in favor of auto detection

	%client.infoRequestMap = "";

	%weapons = dsWeaponManagerSO.list;
	%count = getFieldCount(%weapons);
	%targets = 0;

	for (%i = 0; %i < %count; %i++)
	{
		%state = getField(%targetState, %i);
		%weapon = getField(%weapons, %i);
		%itemName = %weapon.getName();

		%map.target[%itemName] = %state ? 1 : "";
		%map.duels[%itemName] = "";
		%map.goodRatings[%itemName] = "";
		%map.badRatings[%itemName] = "";

		if (%state)
			%targets++;
	}

	if (!%targets)
		return;

	%map.state = 1;
	%map.feedback = "";
	%map.reviewerID = "";
	%map.reviewerName = "";
	%map.saveMap();

	dsMapManagerSO.addSubmission(%map);

	commandToClient(%client, 'dcSetSave', %map, %map.name, 1, 0, 0);
	commandToClient(%client, 'dcConfirmedInfoSubmission');
}

function serverCmddcViewMaps(%client, %idx)
{
	%idx = %idx | 0;
	%weapons = dsWeaponManagerSO.list;
	%count = getFieldCount(%weapons);

	if (%idx < 0 || %idx >= %count)
		return;

	%weapon = getField(%weapons, %idx);
	%itemName = %weapon.getName();

	%maps = %weapon.maps;
	if (!isObject(%maps))
		return;

	%count = %maps.getCount();

	for (%i = 0; %i < %count; %i++)
	{
		%map = %maps.getObject(%i);
		commandToClient(%client, 'dcSetMap', %map, %map.name, %map.goodRatings[%itemName] | 0, %map.badRatings[%itemName] | 0);
	}

	%client.viewMapsTarget = %weapon;
	%client.viewMapsTargetIdx = %idx;
}

function serverCmddcViewMapStats(%client, %map)
{
	if (!isObject(%map) || %map.state != 2)
		return;

	%weapons = dsWeaponManagerSO.list;
	%count = getFieldCount(%weapons);

	for (%i = 0; %i < %count; %i++)
	{
		%weapon = getField(%weapons, %i);
		%itemName = %weapon.getName();

		%targetState = setField(%targetState, %i, %map.target[%itemName] ? 1 : 0);
	}

	%approver = %map.reviewerID SPC %map.reviewerName;

	if (%target = %client.viewMapsTarget)
	{
		%targetName = %target.getName();
		commandToClient(%client, 'dcShowMapStats', %map.name, %map.owners, %map.duels[%targetName] | 0, %targetState, %map.goodRatings[%targetName] | 0, %map.badRatings[%targetName] | 0, %approver, "N/A");
	}
	else
	{
		commandToClient(%client, 'dcShowMapStats', %map.name, %map.owners, 0, %targetState, 0, 0, %approver, "N/A");
	}
}

function serverCmddcViewSubmissionInfo(%client, %map)
{
	if (!%client.isAdmin || !isObject(%map) || %map.state != 1)
		return;

	%weapons = dsWeaponManagerSO.list;
	%count = getFieldCount(%weapons);

	for (%i = 0; %i < %count; %i++)
	{
		%weapon = getField(%weapons, %i);
		%itemName = %weapon.getName();

		%targetState = setField(%targetState, %i, %map.target[%itemName] ? 1 : 0);
	}

	commandToClient(%client, 'dcShowSubmissionInfo', %map.owners, %targetState);
}

function serverCmddcWithdraw(%client, %map)
{
	if (!isObject(%map) || %map.submitterID != %client.bl_id || %map.state < 1)
		return;

	if (%map.state == 2)
	{
		%weapons = dsWeaponManagerSO.list;
		%count = getFieldCount(%weapons);

		for (%i = 0; %i < %count; %i++)
		{
			%weapon = getField(%weapons, %i);
			%itemName = %weapon.getName();

			if (%map.target[%itemName])
				%weapon.maps.remove(%map);
		}
	}
	else
	{
		dsMapManagerSO.removeSubmission(%map);
	}

	%map.state = 0;
	%map.saveMap();

	commandToClient(%client, 'dcSetSave', %map, %map.name, 0, 0, 0);
}

function serverCmdClearAllBricks(%client)
{
	// TODO: Restrict to miniGame

	Parent::serverCmdClearAllBricks(%client);
}

function serverCmdClearBrickGroup(%client, %bl_id)
{
	// TODO: Restrict to miniGame

	Parent::serverCmdClearBrickGroup(%client, %bl_id);
}

function serverCmdClearBricks(%client, %a)
{
	// TODO: Restrict to miniGame

	Parent::serverCmdClearBricks(%client, %a);
}

function serverCmdCreateMiniGame(%client, %title, %colorIdx, %useSpawnBricks)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame))
		return;

	if (serverCmddcStartBuilding(%client))
		commandToClient(%client, 'CreateMiniGameSuccess');
}

function serverCmdRemoveFromMiniGame(%client, %target)
{
	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.duel && !%miniGame.testDuel)
		return;

	Parent::serverCmdRemoveFromMiniGame(%client, %target);
}

function serverCmdAcceptMiniGameInvite(%client, %miniGameInvite)
{
	// Duelists cannot accept invites when in a duel.

	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.duel && !%miniGame.testDuel && (%miniGame.duelist1 == %client || %miniGame.duelist2 == %client))
		return;

	Parent::serverCmdAcceptMiniGameInvite(%client, %miniGameInvite);
}

function serverCmdInviteToMiniGame(%client, %target)
{
	// Prevent invites of people in duels.

	if (!isObject(%client.miniGame))
		return;

	%target = mFloatLength(%target, 0);
	if (!isObject(%target))
		return;

	%miniGame = %target.miniGame;
	if (isObject(%miniGame) && %miniGame.duel && !%miniGame.testDuel)
		return;

	Parent::serverCmdInviteToMiniGame(%client, %target);
}

function GameConnection::onClientEnterGame(%this)
{
	Parent::onClientEnterGame(%this);

	%partition = dsWorldPartitionManagerSO.centerPartition;
	%this.partition = %partition;
	%partition.scopeEnvironment(%this);
	dsChallengeManagerSO.broadcastPlayerUpdate(%this, 0, %this);
	commandToClient(%this, 'dcPing', dsLegacyDataServer.port);
}

function GameConnection::onClientLeaveGame(%this)
{
	if (isObject(%this.challengeList))
		%this.challengeList.delete();

	Parent::onClientLeaveGame(%this);

	dsChallengeManagerSO.broadcastPlayerUpdate(%this, -1, %this);
}

function GameConnection::spawnPlayer(%this)
{
	%miniGame = %this.miniGame;
	if (isObject(%miniGame) && %miniGame.duel && %miniGame.duelist1 != %this && %miniGame.duelist2 != %this)
	{
		// Spectate with camera
		%camera = %this.camera;
		%db = %camera.getDataBlock();
		%this.setControlObject(%camera);
		if (%camera.mode $= "Observer")
		{
			%db.onTrigger(%camera, 2, 1);
			%db.onTrigger(%camera, 2, 0);
		}
		%db.onTrigger(%camera, 0, 1);
		%db.onTrigger(%camera, 0, 0);
		return %this;
	}

	Parent::spawnPlayer(%this);

	// Allow for spawn killing.
	%player = %this.player;
	if (isObject(%player))
	{
		%player.lightCheckSpawnTime = %player.spawnTime;
		%player.spawnTime = "";

		if (isObject(%miniGame) && %miniGame.duel)
		{
			%player.scopeToClient(%miniGame.duelist1);
			%player.scopeToClient(%miniGame.duelist2);
		}
		else
		{
			%player.scopeToClient(%this);
		}
	}

	return %this;
}

function dsLegacyDataServer(%port)
{
	if (isObject(dsLegacyDataServer))
		return nameToID(dsLegacyDataServer);

	%server = new TCPObject(dsLegacyDataServer)
	{
		port = %port;
		dummyStats = "0\tNot Found\t0 0 0 0 0 0 0\t";
	};

	%server.clients = new SimSet();
	%server.listen(%port);

	return %server;
}

function dsLegacyDataServer::onConnectRequest(%this, %address, %id)
{
	%this.clients.add(new TCPObject(dsLegacyDataClient, %id));
}

function dsLegacyDataClient::onLine(%this, %line)
{
	%cmd = getField(%line, 0);

	switch$ (%cmd)
	{
	case "GR":
		%this.send("RD");
	case "ST":
		%id = getField(%line, 1);
		if ((%id | 0) $= %id && %id > 0)
		{
			%record = dsStatManagerSO.getRecordFromID(%id, 0);
			if (%record)
			{
				%stats = %record.bl_id TAB %record.name;
				%losses = %record.losses;
				%wins = %record.wins;
				%kills = %record.kills;
				%deaths = %record.deaths;
				%wlRatio = %losses ? mFloatLength(%wins / %losses, 2) : %wins;
				%kdRatio = %deaths ? mFloatLength(%kills / %deaths, 2) : %kills;
				%stats = %stats TAB %record.duels SPC %wins SPC %losses SPC %wlRatio SPC %kills SPC %deaths SPC %kdRatio;
				%stats = %stats @ "\t";

				%weaponList = dsWeaponManagerSO.list;
				%weaponCount = getFieldCount(%weaponList);
				%first = 1;
				for (%i = 0; %i < %weaponCount; %i++)
				{
					%weapon = getField(%weaponList, %i);
					%itemName = %weapon.getName();
					%duels = %record.duels[%itemName];
					if (%duels !$= "")
					{
						%losses = %record.losses[%itemName];
						%wins = %record.wins[%itemName];
						%kills = %record.kills[%itemName];
						%deaths = %record.deaths[%itemName];
						%wlRatio = %losses ? mFloatLength(%wins / %losses, 2) : %wins;
						%kdRatio = %deaths ? mFloatLength(%kills / %deaths, 2) : %kills;
						%wlRank = "---";
						%kdRank = "---";
						%weaponStats = %weapon.uiName SPC %duels SPC %wins SPC %losses SPC %wlRatio SPC %wlRank SPC %kills SPC %deaths SPC %kdRatio SPC %kdRank;

						if (%first)
						{
							%stats = %stats @ %weaponStats;
							%first = 0;
						}
						else
						{
							%stats = %stats @ "|" @ %weaponStats;
						}
					}
				}

				%this.send("BS" TAB %stats);

				if (%record.DecalName !$= "")
				{
					%avatar = "AP\tDecalName" TAB %record.DecalName @ "\r\n";
					%avatar = %avatar @ "AP\tFaceName" TAB %record.FaceName @ "\r\n";

					%avatar = %avatar @ "AP\tAccentColor" TAB %record.AccentColor @ "\r\n";
					%avatar = %avatar @ "AP\tChestColor" TAB %record.ChestColor @ "\r\n";
					%avatar = %avatar @ "AP\tHatColor" TAB %record.HatColor @ "\r\n";
					%avatar = %avatar @ "AP\tHeadColor" TAB %record.HeadColor @ "\r\n";
					%avatar = %avatar @ "AP\tHipColor" TAB %record.HipColor @ "\r\n";
					%avatar = %avatar @ "AP\tLArmColor" TAB %record.LArmColor @ "\r\n";
					%avatar = %avatar @ "AP\tLHandColor" TAB %record.LHandColor @ "\r\n";
					%avatar = %avatar @ "AP\tLLegColor" TAB %record.LLegColor @ "\r\n";
					%avatar = %avatar @ "AP\tPackColor" TAB %record.PackColor @ "\r\n";
					%avatar = %avatar @ "AP\tRArmColor" TAB %record.RArmColor @ "\r\n";
					%avatar = %avatar @ "AP\tRHandColor" TAB %record.RHandColor @ "\r\n";
					%avatar = %avatar @ "AP\tRLegColor" TAB %record.RLegColor @ "\r\n";
					%avatar = %avatar @ "AP\tSecondPackColor" TAB %record.SecondPackColor @ "\r\n";

					%avatar = %avatar @ "AP\tAccent" TAB %record.Accent @ "\r\n";
					%avatar = %avatar @ "AP\tChest" TAB %record.Chest @ "\r\n";
					%avatar = %avatar @ "AP\tHat" TAB %record.Hat @ "\r\n";
					%avatar = %avatar @ "AP\tHip" TAB %record.Hip @ "\r\n";
					%avatar = %avatar @ "AP\tLArm" TAB %record.LArm @ "\r\n";
					%avatar = %avatar @ "AP\tLHand" TAB %record.LHand @ "\r\n";
					%avatar = %avatar @ "AP\tLLeg" TAB %record.LLeg @ "\r\n";
					%avatar = %avatar @ "AP\tPack" TAB %record.Pack @ "\r\n";
					%avatar = %avatar @ "AP\tRArm" TAB %record.RArm @ "\r\n";
					%avatar = %avatar @ "AP\tRHand" TAB %record.RHand @ "\r\n";
					%avatar = %avatar @ "AP\tRLeg" TAB %record.RLeg @ "\r\n";
					%avatar = %avatar @ "AP\tSecondPack" TAB %record.SecondPack @ "\r\n";

					%this.send("\r\n" @ %avatar @ "AD");
				}
				else
				{
					%this.send("\r\nAR\r\nAD");
				}
			}
			else
			{
				%this.send("AR\r\nAD\r\nBS" TAB dsLegacyDataServer.dummyStats);
			}
		}
		else
		{
			%this.send("AR\r\nAD\r\nBS" TAB dsLegacyDataServer.dummyStats);
		}
	case "WR":
		%this.send("WL" TAB dsWeaponManagerSO.legacyDataServerList);
	default:
		%this.send("PONG");
	}

	%this.send("\r\n");

	if (%cmd !$= "PING")
		%this.delete();
}

function GuiTextListCtrl::dumpRows(%this)
{
	%count = %this.rowCount();
	if (!%count)
		return;

	echo("Rows:");

	for (%i = 0; %i < %count; %i++)
		echo("  " @ %i @ ": " @ %this.getRowId(%i) @ " - " @ %this.getRowText(%i));
}

function dumpChallenges()
{
	%group = nameToID(ClientGroup);
	%count = %group.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%client = %group.getObject(%i);
		%list = %client.challengeList;
		echo(%client.name @ ":");
		if (isObject(%list))
			%list.dumpRows();
	}

	echo("Boasts:");
	dsChallengeManagerSO.boastList.dumpRows();
}

}; // package Server_Dueling
