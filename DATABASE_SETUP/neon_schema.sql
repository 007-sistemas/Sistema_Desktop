-- Script SQL para criar as tabelas no Neon PostgreSQL
-- Execute este script no Neon SQL Editor em: https://console.neon.tech

-- Tabela de usuários
CREATE TABLE IF NOT EXISTS users (
  id SERIAL PRIMARY KEY,
  name VARCHAR(255) NOT NULL,
  email VARCHAR(255) UNIQUE NOT NULL,
  matricula VARCHAR(50) UNIQUE NOT NULL,
  categoria VARCHAR(100),
  biometric_registered BOOLEAN DEFAULT FALSE,
  status VARCHAR(20) DEFAULT 'ATIVO',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de biometrias
CREATE TABLE IF NOT EXISTS biometrics (
  id SERIAL PRIMARY KEY,
  user_id INTEGER NOT NULL UNIQUE,
  biometric_data BYTEA NOT NULL,
  biometric_type VARCHAR(50) DEFAULT 'fingerprint',
  registered_at TIMESTAMP NOT NULL,
  status VARCHAR(20) DEFAULT 'REGISTERED',
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Tabela de registros de ponto
CREATE TABLE IF NOT EXISTS time_records (
  id SERIAL PRIMARY KEY,
  user_id INTEGER NOT NULL,
  record_type VARCHAR(50) NOT NULL,
  recorded_at TIMESTAMP NOT NULL,
  verified BOOLEAN DEFAULT FALSE,
  ip_address VARCHAR(45),
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Criar índices para melhor performance
CREATE INDEX IF NOT EXISTS idx_users_matricula ON users(matricula);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_biometrics_user ON biometrics(user_id);
CREATE INDEX IF NOT EXISTS idx_time_records_user ON time_records(user_id);
CREATE INDEX IF NOT EXISTS idx_time_records_date ON time_records(recorded_at DESC);

-- Inserir usuários de exemplo (opcional)
INSERT INTO users (name, email, matricula, categoria, biometric_registered, status)
VALUES 
  ('Gabriel Gomes da Silva Lima', 'gabriel@example.com', '1', 'Enfermeiro', false, 'ATIVO'),
  ('Leandro Lima', 'leandro@example.com', '100', 'Fisioterapeuta', false, 'ATIVO'),
  ('Agatha Allana Laura dos Santos', 'agatha@example.com', '19', 'Enfermeiro', false, 'ATIVO')
ON CONFLICT DO NOTHING;
