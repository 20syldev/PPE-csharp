-- Créer la base de données
CREATE DATABASE ppe;

-- Créer l'utilisateur
CREATE USER ppe WITH PASSWORD 'votre_mot_de_passe';

-- Donner les droits sur la base
GRANT ALL PRIVILEGES ON DATABASE ppe TO ppe;

-- Se connecter à ppe
\c ppe

-- Table client
CREATE TABLE client (
    id UUID PRIMARY KEY,
    nom VARCHAR(100),
    adresse VARCHAR(200),
    ville VARCHAR(100),
    code VARCHAR(10)
);

-- Droits sur la table et séquence
GRANT ALL PRIVILEGES ON TABLE client TO ppe;

-- Procédure d'ajout d'un client
CREATE OR REPLACE PROCEDURE client_add(
    p_id UUID,
    p_nom VARCHAR(100),
    p_adresse VARCHAR(200),
    p_ville VARCHAR(100),
    p_code VARCHAR(10)
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO client (id, nom, adresse, ville, code)
    VALUES (p_id, p_nom, p_adresse, p_ville, p_code);
END;
$$;

-- Fonction de liste des clients
CREATE OR REPLACE FUNCTION client_list()
RETURNS TABLE (
    id UUID,
    nom VARCHAR(100),
    adresse VARCHAR(200),
    ville VARCHAR(100),
    code VARCHAR(10)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT c.id, c.nom, c.adresse, c.ville, c.code FROM client c ORDER BY c.nom;
END;
$$;

-- Procédure de modification d'un client
CREATE OR REPLACE PROCEDURE client_update(
    p_id UUID,
    p_nom VARCHAR(100),
    p_adresse VARCHAR(200),
    p_ville VARCHAR(100),
    p_code VARCHAR(10)
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE client SET nom = p_nom, adresse = p_adresse, ville = p_ville, code = p_code WHERE id = p_id;
END;
$$;

-- Procédure de suppression d'un client
CREATE OR REPLACE PROCEDURE client_delete(p_id UUID)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM client WHERE id = p_id;
END;
$$;