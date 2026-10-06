-- Benchmark database: who measured (campaign), what (mesh, file), with which library (implementation),
-- and the raw durations (measurement). Constraints reject what a measurement cannot be.

CREATE TABLE campaign (
  campaign_id  integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  run_at       timestamptz NOT NULL UNIQUE,
  runtime      text NOT NULL,
  os           text NOT NULL,
  arch         text NOT NULL,
  cpu          text NOT NULL,
  git_commit   text NOT NULL CHECK (git_commit ~ '^([0-9a-f]{7,40}|unknown)$'),
  warmup       smallint NOT NULL CHECK (warmup >= 0),
  repetitions  smallint NOT NULL CHECK (repetitions >= 1)
);

CREATE TABLE mesh (
  mesh_id         integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  family          text NOT NULL CHECK (family IN ('torus', 'cylinder', 'sphere')),
  resolution      integer NOT NULL CHECK (resolution >= 3),
  vertices        integer NOT NULL CHECK (vertices > 0),
  triangles       integer NOT NULL CHECK (triangles > 0),
  euler           integer NOT NULL,
  boundary_loops  integer NOT NULL CHECK (boundary_loops >= 0),
  UNIQUE (family, resolution),
  -- On a closed triangulated surface every edge has two triangles (3 F = 2 E), so χ = V - E + F = V - F / 2.
  CHECK (boundary_loops > 0 OR (triangles % 2 = 0 AND euler = vertices - triangles / 2))
);

CREATE TABLE file (
  file_id  integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  mesh_id  integer NOT NULL REFERENCES mesh ON DELETE CASCADE,
  format   text NOT NULL CHECK (format IN ('obj', 'ply', 'stl')),
  bytes    integer NOT NULL CHECK (bytes > 0),
  UNIQUE (mesh_id, format)
);

CREATE TABLE implementation (
  implementation_id  integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  name               text NOT NULL UNIQUE,
  language           text NOT NULL,
  work               text NOT NULL
);

CREATE TABLE measurement (
  campaign_id        integer NOT NULL REFERENCES campaign ON DELETE CASCADE,
  file_id            integer NOT NULL REFERENCES file ON DELETE CASCADE,
  implementation_id  integer NOT NULL REFERENCES implementation,
  repetition         smallint NOT NULL CHECK (repetition >= 1),
  duration_ms        double precision NOT NULL CHECK (duration_ms > 0 AND duration_ms < 'Infinity'),
  PRIMARY KEY (campaign_id, file_id, implementation_id, repetition)
);

-- A repetition number cannot exceed what its campaign announced: a CHECK cannot read another table,
-- so a trigger enforces it.
CREATE FUNCTION check_repetition() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.repetition > (SELECT repetitions FROM campaign WHERE campaign_id = NEW.campaign_id) THEN
    RAISE EXCEPTION 'repetition % exceeds the repetitions of campaign %', NEW.repetition, NEW.campaign_id
      USING ERRCODE = 'check_violation';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER measurement_repetition BEFORE INSERT OR UPDATE ON measurement
  FOR EACH ROW EXECUTE FUNCTION check_repetition();

INSERT INTO implementation (name, language, work) VALUES
  ('lib-c', 'C11 (WebAssembly)', 'read the file, count edges and boundary edges'),
  ('topologie', 'C++20 (WebAssembly)', 'read the file, build the half-edge structure, invariants and curvature');
