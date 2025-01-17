﻿using System.Data;
using System.Globalization;
using System.Text.Json;
using CsvHelper;
using Dapper;
using Figgle;
using MySql.Data.MySqlClient;
using RezultateVotDataExtractor;
using RezultateVotDataExtractor.Views;

Console.WriteLine(FiggleFonts.Ogre.Render("Data extractor"));

string connectionString = "Server=localhost;Port=3306;Database=latest;Uid=root;Pwd=root;";
string listElectionsQuery = @"SELECT e.Name ElectionName,
       CASE
            WHEN e.Category = 0 THEN ""Referendum""
            WHEN e.Category = 1 THEN ""Presidential""
            WHEN e.Category = 2 THEN ""Local""
            WHEN e.Category = 3 THEN ""Parliament""
            WHEN e.Category = 4 THEN ""EuropeanParliament""
        
            ELSE ""Unknown""
        END Category, 
       b.name BallotName,
       b.BallotId,
       b.BallotType,
       e.ElectionId,
       e.Date
FROM ballots b
inner join elections e on e.ElectionId = b.ElectionId
where e.ElectionId = 50
ORDER BY e.Date DESC";

string voteParticipationPerLocalityQuery = @"
SELECT  
       b.BallotId,
       e.ElectionId,
       b.name BallotName,
       e.Category,
       e.Name ElectionName,
       e.Subtitle,
       e.Date ElectionDate,
       c.Name County,
       l.Name Locality,
       b.Round,
       b.date BallotDate,
       t.EligibleVoters ,
       t.TotalVotes,
       t.ValidVotes,
       t.NullVotes
FROM turnouts t
inner join localities l on l.LocalityId = t.LocalityId
inner join counties c on c.CountyId = t.CountyId
inner join ballots b on b.BallotId = t.BallotId
inner join elections e on e.ElectionId = b.ElectionId
where e.ElectionId = @ElectionId
    and b.BallotId = @BallotId
order by c.Name Asc,
         l.Name asc";

var partiesResultsPerLocalityQuery = @"
select e.ElectionId,
        b.BallotId,
        c.Name County,
        l.Name Locality,
        cr.Name Candidate,
        COALESCE(p.Name, 'CANDIDAT INDEPENDENT') as PartyName,
        COALESCE(p.ShortName, '') ShortName,
        COALESCE(p.Alias, '') Alias,
        cr.Votes,
        cr.SeatsGained,
        cr.TotalSeats,
        cr.Seats1,
        cr.Seats2,
        cr.OverElectoralThreshold
from candidateresults cr
inner join localities l on l.LocalityId = cr.LocalityId
inner join counties c on c.CountyId = cr.CountyId
inner join ballots b on cr.BallotId = b.BallotId
inner join elections e on b.ElectionId = e.ElectionId
left join parties p on p.Id = cr.PartyId

where e.ElectionId = @ElectionId
    and b.BallotId = @BallotId
order by e.date desc,
         c.Name Asc,
         l.Name asc
";

var partiesResultsPerCountyQuery = @"
select e.ElectionId,
        b.BallotId,
        c.Name County,
        cr.Name Candidate,
        COALESCE(p.Name, 'CANDIDAT INDEPENDENT') as PartyName,
        COALESCE(p.ShortName, '') ShortName,
        COALESCE(p.Alias, '') Alias,
        cr.Votes,
        cr.SeatsGained,
        cr.TotalSeats,
        cr.Seats1,
        cr.Seats2,
        cr.OverElectoralThreshold
from candidateresults cr
inner join counties c on c.CountyId = cr.CountyId
inner join ballots b on cr.BallotId = b.BallotId
inner join elections e on b.ElectionId = e.ElectionId
left join parties p on p.Id = cr.PartyId

where e.ElectionId = @ElectionId
    and b.BallotId = @BallotId
order by e.date desc,
         c.Name Asc
";

// Diaspora queries
string voteParticipationPerCountry = @"
SELECT  
       b.BallotId,
       e.ElectionId,
       b.name BallotName,
       e.Category,
       e.Name ElectionName,
       e.Subtitle,
       e.Date ElectionDate,
       c.Name Country,
       b.Round,
       b.date BallotDate,
       t.EligibleVoters ,
       t.TotalVotes,
       t.ValidVotes,
       t.NullVotes
FROM turnouts t
inner join countries c on c.Id = t.CountryId
inner join ballots b on b.BallotId = t.BallotId
inner join elections e on e.ElectionId = b.ElectionId
where e.ElectionId = @ElectionId
    and b.BallotId = @BallotId
order by c.Name Asc";

var partiesResultsPerCountry = @"
select e.ElectionId,
        b.BallotId,
        c.Name Country,
        cr.Name Candidate,
        COALESCE(p.Name, 'CANDIDAT INDEPENDENT') as PartyName,
        COALESCE(p.ShortName, '') ShortName,
        COALESCE(p.Alias, '') Alias,
        cr.Votes,
        cr.SeatsGained,
        cr.TotalSeats,
        cr.Seats1,
        cr.Seats2,
        cr.OverElectoralThreshold
from candidateresults cr
inner join countries c on c.Id = cr.CountryId
inner join ballots b on cr.BallotId = b.BallotId
inner join elections e on b.ElectionId = e.ElectionId
left join parties p on p.Id = cr.PartyId
where e.ElectionId = @ElectionId
    and b.BallotId = @BallotId
order by e.date desc,
         c.Name Asc
";

static void WriteToCSV<T>(IEnumerable<T> records, string fileName)
{
    var path = Path.Combine("data", "csv", fileName);

    using (var writer = new StreamWriter(path))
    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
    {
        csv.WriteRecords(records);
    }
}

static void WriteToJson<T>(IEnumerable<T> records, string fileName)
{
    var path = Path.Combine("data", "json", fileName);

    string jsonString = JsonSerializer.Serialize(records, new JsonSerializerOptions() { WriteIndented = true });

    File.WriteAllText(path, jsonString);
}

using IDbConnection dbConnection = new MySqlConnection(connectionString);
try
{
    dbConnection.Open();
    var elections = await dbConnection.QueryAsync<ElectionView>(listElectionsQuery);
    WriteToCSV(elections, "elections-list.csv");
    WriteToJson(elections, "elections-list.json");

    foreach (var election in elections)
    {
        var parameters = new { election.ElectionId, election.BallotId };
        // romania
        var electionAttendanceInRomania = await dbConnection.QueryAsync<ElectionAttendanceInRomaniaView>(voteParticipationPerLocalityQuery, parameters);

        WriteToCSV(electionAttendanceInRomania, $"elections-attendance-romania-{election.ElectionId}-{election.BallotId}.csv");
        WriteToJson(electionAttendanceInRomania, $"elections-attendance-romania-{election.ElectionId}-{election.BallotId}.json");

        IEnumerable<PartiesResultInRomaniaView> partiesResults = [];
        
        if (election.BallotType is (int)BallotType.LocalCouncil or (int)BallotType.Mayor)
        {
            partiesResults = await dbConnection.QueryAsync<PartiesResultInRomaniaView>(partiesResultsPerLocalityQuery, parameters);
        }
        else
        {
            partiesResults = await dbConnection.QueryAsync<PartiesResultInRomaniaView>(partiesResultsPerCountyQuery, parameters);
        }

        WriteToCSV(partiesResults, $"parties-results-romania-{election.ElectionId}-{election.BallotId}.csv");
        WriteToJson(partiesResults, $"parties-results-romania-{election.ElectionId}-{election.BallotId}.json");
        
        // diaspora
        var electionAttendanceInDiaspora = await dbConnection.QueryAsync<ElectionAttendanceInDiasporaView>(voteParticipationPerCountry, parameters);

        WriteToCSV(electionAttendanceInDiaspora, $"elections-attendance-diaspora-{election.ElectionId}-{election.BallotId}.csv");
        WriteToJson(electionAttendanceInDiaspora, $"elections-attendance-diaspora-{election.ElectionId}-{election.BallotId}.json");

        var partyResultsInDiaspora = await dbConnection.QueryAsync<PartiesResultInDiasporaView>(partiesResultsPerCountry, parameters);

        WriteToCSV(partyResultsInDiaspora, $"parties-results-diaspora-{election.ElectionId}-{election.BallotId}.csv");
        WriteToJson(partyResultsInDiaspora, $"parties-results-diaspora-{election.ElectionId}-{election.BallotId}.json");
    }

    Console.WriteLine("Done!");
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}