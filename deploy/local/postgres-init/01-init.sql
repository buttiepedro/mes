-- Se ejecuta una sola vez, en la primera inicialización del contenedor de Postgres.
-- La DB del Control Plane (nexo_controlplane) la crea POSTGRES_DB.
-- Acá creamos la DB del tenant demo para poder probar el slice de Producción localmente.
--
-- NOTA: en producción cada tenant tiene su PROPIO proyecto Neon (ver docs/design/01-multi-tenancy-connection.md).
-- Este archivo es solo para el entorno local de pruebas (una sola instancia Postgres con varias DBs).

CREATE DATABASE nexo_tenant_demo;
