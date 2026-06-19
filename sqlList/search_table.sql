select * from UserData;

select * from StageClearData;

select scd.UserId, scd.StageIdx, scd.ClearState from StageClearData scd 
join UserData ud on scd.UserId = ud.UserId 
where ud.UserId = 1 and scd.StageIdx > ud.SavedStageIdx;

