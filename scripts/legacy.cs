// Copyright (c) Johnny Patterson

// commandToClient(%client, 'dcAlertMsg', %msg);
// commandToClient(%client, 'dcConfirmedInfoSubmission');
// commandToClient(%client, 'dcLoadBackup');
// commandToClient(%client, 'dcMapRating');
// commandToClient(%client, 'dcOverwrite');
// commandToClient(%client, 'dcRefreshMaps', %weapon);
// commandToClient(%client, 'dcRemoveMap', %id);
// commandToClient(%client, 'dcRemoveSave', %id);
// commandToClient(%client, 'dcRemoveSubmission', %id);
// commandToClient(%client, 'dcRequestInfo', %save, %tags, %contributors);
// commandToClient(%client, 'dcSetBrickCount', %count, %max);
// commandToClient(%client, 'dcSetMap', %id, %name, %goodRatings, %badRatings);
// commandToClient(%client, 'dcSetSave', %id, %name, %status, %goodRatings, %badRatings);
// commandToClient(%client, 'dcSetSubmission', %id, %name);
// commandToClient(%client, 'dcShowMapStats', %name, %builders, %duels, %tags, %goodRatings, %badRatings, %approver, %id);
// commandToClient(%client, 'dcShowSubmissionInfo', %builders, %tags);

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

function serverCmddcAlertMsgConfirm(%client, %id)
{
}

function serverCmddcAlertMsgRequest(%client, %id)
{
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
}

function serverCmddcClearBricks(%client)
{
}

function serverCmddcDecline(%client, %source)
{
	if (!isObject(%client.challengeList))
		return;

	%source = mFloatLength(%source, 0) * -1;

	dsChallengeManagerSO.removeChallenge(%source, %client);
}

function serverCmddcDeleteSave(%client, %id)
{
}

function serverCmddcEditInfo(%client, %id)
{
}

function serverCmddcLoadBackup(%client, %yes)
{
}

function serverCmddcLoadMap(%client, %id)
{
}

function serverCmddcLoadSave(%client, %id)
{
}

function serverCmddcLoadSubmission(%client, %id)
{
}

function serverCmddcMapRating(%client, %good)
{
}

function serverCmddcOverwrite(%client)
{
}

function serverCmddcPong(%client, %version, %revision)
{
	%client.dcClient = 1;
	commandToAllExcept(%client, 'dcSetPlayer', %client, %client.name, 1);
	serverCmddcRequestTransmission(%client);
}

function serverCmddcRequestTransmission(%client)
{
	if (isObject(%client.minigame))
	{
		%building = %client.miniGame.buildSession;
		%dueling = !%building && %client.miniGame.duel;

		if (%building && %client.miniGame.owner == %client)
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

	%group = nameToID(ClientGroup);
	%count = %group.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%obj = %group.getObject(%i);
		if (%obj == %client)
			continue;

		%status = %obj.dcClient ? 1 : 0;
		commandToClient(%client, 'dcSetPlayer', %obj, %obj.name, %status);
	}

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
}

function serverCmddcReviewSubmission(%client, %id, %accept, %declineMessage)
{
}

function serverCmddcRevokeMap(%client, %id, %message)
{
}

function serverCmddcSaveBricks(%client, %name)
{
	// commandToClient(%client, 'dcSaveExists');
	// commandToClient(%client, 'dcSaveLimit');
}

function serverCmddcSpectate(%client, %id)
{
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

	%partition = dsWorldPartitionManagerSO.allocate();
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
		if (!%minigame.ending)
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

function serverCmddcSubmit(%client, %id)
{
}

function serverCmddcSubmitInfo(%client, %tags, %builders)
{
}

function serverCmddcSubmitInfo(%client, %tags, %builders)
{
}

function serverCmddcViewMaps(%client, %id)
{
}

function serverCmddcViewMapStats(%client, %id)
{
}

function serverCmddcViewSubmissionInfo(%client, %id)
{
}

function serverCmddcWithdraw(%client, %id)
{
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

function serverCmdAcceptMiniGameInvite(%client, %miniGame)
{
	// Duelists cannot accept invites when in a duel.

	%miniGame = %client.miniGame;
	if (isObject(%miniGame) && %miniGame.duel && !%miniGame.testDuel && (%miniGame.duelist1 == %client || %miniGame.duelist2 == %client))
		return;

	Parent::serverCmdAcceptMiniGameInvite(%client, %miniGame);
}

function serverCmdInviteToMiniGame(%client, %target)
{
	// Prevent invites of people in duels.

	if (!isObject(%client.miniGame))
		return;

	%target = mFloatLength(%target, 0);
	if (!isObject(%target))
		return;

	%miniGame = %target.minigame;
	if (isObject(%miniGame) && %miniGame.duel && !%miniGame.testDuel)
		return;

	Parent::serverCmdInviteToMiniGame(%client, %target);
}

function GameConnection::onClientEnterGame(%this)
{
	Parent::onClientEnterGame(%this);

	commandToClient(%this, 'dcPing', dsLegacyDataServer.port);
	commandToAllExcept(%this, 'dcSetPlayer', %this, %this.name, 0);
}

function GameConnection::onClientLeaveGame(%this)
{
	if (isObject(%this.challengeList))
		%this.challengeList.delete();

	commandToAllExcept(%this, 'dcRemovePlayer', %this);

	Parent::onClientLeaveGame(%this);
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
		%player.spawnTime = "";

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
		if ((%id * 1) $= %id && %id > 0)
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
