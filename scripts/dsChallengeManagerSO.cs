// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsChallengeManagerSO()
{
	if (isObject(dsChallengeManagerSO))
		return nameToID(dsChallengeManagerSO);

	%obj = new ScriptObject(dsChallengeManagerSO)
	{
		minGoal = 3;
		maxGoal = 9;
		suddenDeathScore = 15;
	};

	%obj.boastList = new GuiTextListCtrl();
	%obj.duelPaneList = new SimSet();

	return %obj;
}

function dsChallengeManagerSO::accept(%this, %client, %source)
{
	%challenge = %source < 0 ? 1 : 0;

	if (%challenge)
	{
		if (!isObject(%client.challengeList))
			return 0;

		%list = %client.challengeList;
		%source = ~%source;
	}
	else
	{
		%list = %this.boastList;
	}

	%rowIdx = %list.getRowNumById(%source);
	if (%rowIdx == -1)
		return 0;

	%info = %list.getRowText(%rowIdx);

	if (%source == %client)
		return 0;

	%result = %this.startDuel(%source, %client, getField(%info, 2), getField(%info, 1), %challenge);

	if (%result > 0)
	{
		if (%challenge)
			%this.removeChallenge(%source, %client);
		else
			%this.removeBoast(%source);

		%this.cancelChallengesTo(%client);
		%this.cancelChallengesTo(%source);
		%this.cancelChallengeFrom(%client);
	}

	return %result;
}

function dsChallengeManagerSO::addBoast(%this, %client, %weapon, %goal)
{
	%info = %weapon.uiName TAB %goal TAB %weapon;

	%client.challenging = 2;
	%client.challengeInfo = %info;

	%list = %this.boastList;
	%list.addRow(%client, %info);
	%list.sort(0, 1);

	if (isObject(%client) && !%client.isAIControlled())
		commandToClient(%client, 'dcSetChallenging', 1, %weapon.uiName, %goal, 0);

	commandToAllExcept(%client, 'dcSetChallenge', %client, %weapon.uiName, %goal, 0);
}

function dsChallengeManagerSO::removeBoast(%this, %client)
{
	%list = %this.boastList;
	%rowIdx = %list.getRowNumById(%client);

	if (%rowIdx == -1)
		return;

	%list.removeRow(%rowIdx);

	if (isObject(%client) && !%client.isAIControlled())
		commandToClient(%client, 'dcSetChallenging', 0);

	commandToAllExcept(%client, 'dcRemoveChallenge', %client);

	%client.challenging = "";
	%client.challengeInfo = "";
}

function dsChallengeManagerSO::broadcastPlayerUpdate(%this, %client, %status, %ignore)
{
	%list = %this.duelPaneList;
	%count = %list.getCount();

	for (%i = 0; %i < %count; %i++)
	{
		%obj = %list.getObject(%i);

		if (%obj != %ignore && !%obj.isAIControlled())
		{
			if (%status < 0)
				commandToClient(%obj, 'dcRemovePlayer', %client);
			else
				commandToClient(%obj, 'dcSetPlayer', %client, %client.name, %status);
		}
	}
}

function dsChallengeManagerSO::addChallenge(%this, %client, %target, %weapon, %goal)
{
	%list = %target.challengeList;

	if (!isObject(%list))
	{
		%list = new GuiTextListCtrl();
		%target.challengeList = %list;
	}

	%count = %list.rowCount();
	%info = %weapon.uiName TAB %goal TAB %weapon;

	%list.addRow(%client, %info);
	%list.sort(0, 1);

	%client.challenging = 1;
	%client.challengeInfo = %info;
	%client.challengeTarget = %target;

	if (isObject(%client) && !%client.isAIControlled())
		commandToClient(%client, 'dcSetChallenging', 1, %weapon.uiName, %goal, 1, %target.name);

	if (isObject(%target) && !%target.isAIControlled())
		commandToClient(%target, 'dcSetChallenge', ~%client, %weapon.uiName, %goal, 1, %client.name);
}

function dsChallengeManagerSO::removeChallenge(%this, %client, %target)
{
	%list = %target.challengeList;

	if (isObject(%list))
	{
		%count = %list.rowCount();
		%rowIdx = %list.getRowNumById(%client);

		if (%rowIdx != -1)
		{
			%list.removeRow(%rowIdx);

			if (isObject(%target) && !%target.isAIControlled())
				commandToClient(%target, 'dcRemoveChallenge', ~%client);
		}
	}

	if (isObject(%client) && !%client.isAIControlled())
		commandToClient(%client, 'dcSetChallenging', 0);

	%client.challenging = "";
	%client.challengeInfo = "";
	%client.challengeTarget = "";
}

function dsChallengeManagerSO::cancelChallengesTo(%this, %client)
{
	%list = %client.challengeList;
	if (!isObject(%list))
		return;

	while (%count = %list.rowCount())
	{
		%source = %list.getRowId(%count - 1);
		%this.removeChallenge(%source, %client);
		messageClient(%source, '', "\c3" @ %client.name @ " \c2rejected your challenge!");
	}
}

function dsChallengeManagerSO::cancelChallengeFrom(%this, %client)
{
	if (%client.challenging == 2)
	{
		%this.removeBoast(%client);
	}
	else if (%client.challenging == 1)
	{
		messageClient(%client.challengeTarget, '', "\c3" @ %client.name @ " \c2cancelled their challenge!");
		messageClient(%client, '', "\c2You cancelled your challenge!");
		%this.removeChallenge(%client, %client.challengeTarget);
	}
}

function dsChallengeManagerSO::startDuel(%this, %duelist1, %duelist2, %weapon, %goal, %practice)
{
	%partition = dsWorldPartitionManagerSO.acquire();
	if (!isObject(%partition))
		return -1;

	if (isObject(%maps = %weapon.maps))
	{
		%count = %maps.getCount();
		%map = %count ? %maps.getObject(getRandom(0, %count - 1)) : "";
	}

	%result = %partition.hostDuelingSession(%duelist1, %duelist2, %weapon, %goal, %practice, %map);
	if (%result <= 0)
		return 0;

	return 1;
}

}; // package Server_Dueling
