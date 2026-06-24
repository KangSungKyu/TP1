
/*
create table UserData (
UserId int primary key auto_increment,
clientId varchar(40) not null unique,
Level int default 1,
Exp int default 0,
MapIdx int default 6001,
StageIdx int default 7001,
SavedMapIdx int default 6001,
SavedStageIdx int default 0
);
//*/

/*
create table StageClearData (
UserId int not null,
StageIdx int not null,
ClearState int default '0',

primary key(UserId, StageIdx),
foreign key(UserId) references UserData(UserId) on delete cascade
);
//*/

/*
create table UserSkillData (
UserId int not null,
SkillIdx int not null,
Slot int default '0',

primary key(UserId, SkillIdx),
foreign key(UserId) references UserData(UserId) on delete cascade,
);
//*/