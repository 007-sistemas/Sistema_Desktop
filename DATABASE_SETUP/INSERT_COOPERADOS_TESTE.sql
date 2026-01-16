-- ============================================
-- SCRIPT RÁPIDO: CRIAR E POPULAR TABELA
-- ============================================
-- Execute TUDO isto no SQL Editor do NEON

-- 1. CRIAR TABELA (se não existir)
CREATE TABLE IF NOT EXISTS cooperados (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome TEXT NOT NULL,
    cpf TEXT UNIQUE,
    email TEXT UNIQUE,
    telefone TEXT,
    criado_em TIMESTAMP DEFAULT NOW(),
    ativo BOOLEAN DEFAULT true
);

-- 2. CRIAR ÍNDICES PARA PERFORMANCE
CREATE INDEX IF NOT EXISTS idx_cooperados_ativo ON cooperados(ativo);
CREATE INDEX IF NOT EXISTS idx_cooperados_nome ON cooperados(nome);
CREATE INDEX IF NOT EXISTS idx_cooperados_cpf ON cooperados(cpf);

-- 3. LIMPAR DADOS EXISTENTES (OPCIONAL - descomente se necessário)
-- DELETE FROM cooperados;

-- 4. INSERIR DADOS DE TESTE
INSERT INTO cooperados (nome, cpf, email, telefone, ativo) 
VALUES 
    ('Gabriel Gomes da Silva Lima', '12345678901', 'gabriel.silva@hospital.com', '(11) 98765-4321', true),
    ('Leandro Lima', '98765432100', 'leandro.lima@hospital.com', '(11) 99876-5432', true),
    ('Agatha Allana Laura dos Santos', '45678901234', 'agatha.santos@hospital.com', '(11) 97654-3210', true),
    ('Ayla Andrea Eliane da Silva', '56789012345', 'ayla.silva@hospital.com', '(11) 96543-2109', true),
    ('Victor Benedito Rezende', '67890123456', 'victor.rezende@hospital.com', '(11) 95432-1098', true),
    ('Hugo Breno André Silva', '78901234567', 'hugo.silva@hospital.com', '(11) 94321-0987', true),
    ('Thales Iago Kauê de Paula', '89012345678', 'thales.paula@hospital.com', '(11) 93210-9876', true),
    ('Beatriz Nina Vanessa Ramos', '90123456789', 'beatriz.ramos@hospital.com', '(11) 92109-8765', true),
    ('Valentina Raquel Laura Drumond', '01234567890', 'valentina.drumond@hospital.com', '(11) 91098-7654', true),
    ('Regina Marli Vieira', '12345678902', 'regina.vieira@hospital.com', '(11) 90987-6543', true),
    ('Breno Kaique da Costa', '23456789012', 'breno.costa@hospital.com', '(81) 98876-5432', true),
    ('Manoel Raimundo das Neves', '34567890123', 'manoel.neves@hospital.com', '(85) 99876-5432', true),
    ('Analu Mirella Sales', '45678901235', 'analu.sales@hospital.com', '(21) 98765-4321', true),
    ('Roberto Osvaldo Rocha', '56789012346', 'roberto.rocha@hospital.com', '(31) 97654-3210', true),
    ('Lavínia Ana Corte Real', '67890123457', 'lavinia.real@hospital.com', '(47) 96543-2109', true),
    ('Nathan Bernardo Ferreira', '78901234568', 'nathan.ferreira@hospital.com', '(61) 95432-1098', true),
    ('Liz Milena Mendes', '89012345679', 'liz.mendes@hospital.com', '(91) 94321-0987', true),
    ('Yasmin Lima do Nascimento', '90123456780', 'yasmin.nascimento@hospital.com', '(92) 93210-9876', true),
    ('Cecilia Eliane Pires', '01234567891', 'cecilia.pires@hospital.com', '(48) 92109-8765', true),
    ('Henrique Marcos Ferreira', '12345678903', 'henrique.ferreira@hospital.com', '(92) 91098-7654', true),
    ('Milena Isabela Novaes', '23456789013', 'milena.novaes@hospital.com', '(79) 90987-6543', true),
    ('Sebastião Carlos Eduardo', '34567890124', 'sebastiao.eduardo@hospital.com', '(83) 89876-5432', true),
    ('Thomas Geraldo Almeida', '45678901236', 'thomas.almeida@hospital.com', '(84) 88765-4321', true),
    ('Breno Kaique da Costa', '56789012347', 'breno2.costa@hospital.com', '(31) 87654-3210', true),
    ('Murilo Sebastião Teixeira', '67890123458', 'murilo.teixeira@hospital.com', '(82) 86543-2109', true)
ON CONFLICT DO NOTHING;

-- 5. VERIFICAR INSERÇÃO
SELECT COUNT(*) as total_cooperados FROM cooperados;
SELECT COUNT(*) as cooperados_ativos FROM cooperados WHERE ativo = true;

-- 6. LISTAR ALGUNS EXEMPLOS
SELECT id, nome, cpf, email FROM cooperados WHERE ativo = true LIMIT 10;

-- ============================================
-- PRONTO!
-- Agora clique em "👆 Cadastrar Biometria"
-- na aplicação e a lista deve aparecer
-- ============================================
