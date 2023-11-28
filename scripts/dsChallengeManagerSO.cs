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
		winBy = 2;
	};

	%obj.boastList = new GuiTextListCtrl();

	return %obj;
}

function dsChallengeManagerSO::accept(%this, %client, %id)
{
	%challenge = %id < 0 ? 1 : 0;

	if (%challenge)
	{
		if (!isObject(%client.challengeList))
			return 0;

		%list = %client.challengeList;
		%id = -1 * %id;
	}
	else
	{
		%list = %this.boastList;
	}

	%rowIdx = %list.getRowNumById(%id);
	if (%rowIdx == -1)
		return 0;

	%source = %list.getRowId(%rowIdx);
	if (%source == %client)
		return 0;

	%info = %list.getRowText(%rowIdx);
	%result = %this.startDuel(%source, %client, getField(%info, 2), getField(%info, 1), %challenge);

	if (%result > 0)
	{
		if (%challenge)
			%this.removeChallenge(%source, %client);
		else
			%this.removeBoast(%source);
	}

	return %result;
}

function dsChallengeManagerSO::addBoast(%this, %client, %weapon, %goal)
{
	%info = %weapon.uiName TAB %goal TAB %weapon;

	%client.challenging = 2;
	%client.challengeInfo = %info;

	%this.boastList.addRow(%client, %info);
	%this.boastList.sort(0, 1);

	if (isObject(%client) && !%client.isAIControlled())
		commandToClient(%client, 'dcSetChallenging', 1, %weapon.uiName, %goal, 0);

	commandToAllExcept(%client, 'dcSetChallenge', %client, %weapon.uiName, %goal, 0);
}

function dsChallengeManagerSO::removeBoast(%this, %client)
{
	if (%this.boastList.getRowNumById(%client) == -1)
		return;

	%this.boastList.removeRowById(%client);

	if (isObject(%client) && !%client.isAIControlled())
		commandToClient(%client, 'dcSetChallenging', 0);
	commandToAllExcept(%client, 'dcRemoveChallenge', %client);

	%client.challenging = "";
	%client.challengeInfo = "";
}

function dsChallengeManagerSO::addChallenge(%this, %client, %target, %weapon, %goal)
{
	if (!isObject(%target.challengeList))
		%target.challengeList = new GuiTextListCtrl();

	%info = %weapon.uiName TAB %goal TAB %weapon;

	%client.challenging = 1;
	%client.challengeInfo = %info;
	%client.challengeTarget = %target;

	%target.challengeList.addRow(%client, %info);
	%target.challengeList.sort(0, 1);

	if (isObject(%client) && !%client.isAIControlled())
		commandToClient(%client, 'dcSetChallenging', 1, %weapon.uiName, %goal, 1, %target.name);
	if (isObject(%target) && !%target.isAIControlled())
		commandToClient(%target, 'dcSetChallenge', -1 * %client, %weapon.uiName, %goal, 1, %client.name);
}

function dsChallengeManagerSO::removeChallenge(%this, %client, %target)
{
	if (!isObject(%target.challengeList) || %target.challengeList.getRowNumById(%client) == -1)
		return;

	%target.challengeList.removeRowById(%client);

	if (isObject(%target) && !%target.isAIControlled())
		commandToClient(%target, 'dcRemoveChallenge', -1 * %client);
	if (isObject(%client) && !%client.isAIControlled())
		commandToClient(%client, 'dcSetChallenging', 0);

	%client.challenging = "";
	%client.challengeInfo = "";
	%client.challengeTarget = "";
}

function dsChallengeManagerSO::cancelChallenge(%this, %client)
{
	if (%client.challenging == 2)
		%this.removeBoast(%client);
	else if (%client.challenging == 1)
		%this.removeChallenge(%client, %client.challengeTarget);
}

function dsChallengeManagerSO::startDuel(%this, %duelist1, %duelist2, %weapon, %goal, %practice)
{
	%partition = dsWorldPartitionManagerSO.acquire();
	if (!isObject(%partition))
	{
		return -1;
	}

	%result = %partition.hostDuelingSession(%duelist1, %duelist2, %weapon, %goal, %practice);
	if (%result <= 0)
		return 0;

	// TODO: Select map and load it
	// TODO: On map loaded, start duel

	return 1;
}

}; // package Server_Dueling
