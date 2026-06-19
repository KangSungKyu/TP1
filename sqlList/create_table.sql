
/*
create table UserData (
UserId int primary key auto_increment,
clientId varchar(40) not null unique,
Level int default 1,
Exp int default 0,
MapIdx int default 6001,
StageIdx int default 7001,
SavedMapIdx int default 6001,
SavedStageIdx int default 7001
);
//*/

/*
create table StageClearData (
StageClearId int primary key auto_increment,
UserId int not null,
StageIdx int,
ClearState int default 0,

foreign key(UserId) references UserData(UserId) on delete cascade,
unique key idx_user_stage (UserId, StageIdx)
);
//*/