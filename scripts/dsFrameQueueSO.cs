// Copyright (c) Johnny Patterson

package Server_Dueling {

function dsFrameQueueSO()
{
	if (isObject(dsFrameQueueSO))
		return nameToID(dsFrameQueueSO);

	%obj = new ScriptObject(dsFrameQueueSO)
	{
		head = 0;
		lastID = 0;
	};

	%obj.list = new GuiTextListCtrl();

	return %obj;
}

function dsFrameQueueSO::process(%this)
{
	if (isEventPending(%this.processEvent))
		return;

	if (isEventPending(%this.waitEvent))
	{
		%this.processEvent = %this.schedule(1, process);
		return;
	}

	%list = %this.list;
	%head = %this.head;
	%count = %list.rowCount() - %head;

	if (%count > 0)
	{
		%task = %list.getRowText(%head);
		%this.waitEvent = eval(%task);
	}

	if (%count <= 1)
	{
		%list.clear();
		%this.head = 0;
		%this.lastID = 0;
	}
	else
	{
		%this.head++;
		%this.processEvent = %this.schedule(1, process);
	}
}

function dsFrameQueueSO::push(%this, %task)
{
	%this.list.addRow(%this.lastID++, %task);
}

function dsFrameQueueSO::pushCritical(%this, %task)
{
	%this.list.addRow(%this.lastID++, %task, %this.head);
}

function dsFrameQueueSO::append(%this, %task)
{
	%list = %this.list;
	%count = %list.rowCount();

	if (%count)
	{
		%last = %count - 1;
		%id = %list.getRowId(%last);
		%list.setRowById(%id, %list.getRowText(%last) @ %task);
	}
	else
	{
		%this.list.addRow(%this.lastID++, %task);
	}
}

}; // package Server_Dueling
