-- The primary key of measurement starts with campaign_id: it already serves "everything a campaign
-- measured" and the cascade from campaign. The history of one file across campaigns (regression
-- checks, the scaling of one mesh over time) filters on file_id and implementation_id instead, and
-- would scan the whole table without this index. tests/pgtap/04_indexes.sql checks the plan.
CREATE INDEX measurement_file_history ON measurement (file_id, implementation_id, campaign_id);

-- file(mesh_id) is covered by UNIQUE (mesh_id, format); mesh(family, resolution) by its UNIQUE constraint.
