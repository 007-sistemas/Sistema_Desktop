-- Verificar estrutura da tabela 'cooperados' no NEON
-- Execute este script para validar se a tabela está corretamente estruturada

-- 1. Verificar se a tabela existe
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public' AND table_name = 'cooperados';

-- 2. Verificar estrutura das colunas
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'cooperados'
ORDER BY ordinal_position;

-- 3. Contar cooperados cadastrados
SELECT COUNT(*) as total_cooperados FROM cooperados;

-- 4. Contar cooperados ativos
SELECT COUNT(*) as cooperados_ativos FROM cooperados WHERE ativo = true;

-- 5. Listar alguns cooperados (primeiros 10)
SELECT id, nome, cpf, email, telefone, criado_em, ativo
FROM cooperados
WHERE ativo = true
ORDER BY nome ASC
LIMIT 10;

-- 6. Verificar se as colunas necessárias existem
-- Estas são as colunas que a aplicação espera:
-- - id (identificador único)
-- - nome (nome do cooperado)
-- - cpf (CPF - opcional)
-- - email (email - opcional)
-- - telefone (telefone - opcional)
-- - criado_em (data de criação - opcional)
-- - ativo (status ativo/inativo)

-- 7. Exemplo: Inserir um novo cooperado (se necessário para testes)
-- INSERT INTO cooperados (id, nome, cpf, email, telefone, criado_em, ativo)
-- VALUES (
--     '123e4567-e89b-12d3-a456-426614174000',
--     'João Silva',
--     '12345678901',
--     'joao.silva@hospital.com',
--     '(11) 98765-4321',
--     NOW(),
--     true
-- );

-- 8. Procurar cooperados por nome (exemplo)
-- SELECT * FROM cooperados 
-- WHERE nome ILIKE '%Silva%' AND ativo = true
-- ORDER BY nome ASC;

-- 9. Procurar cooperados por CPF (exemplo)
-- SELECT * FROM cooperados 
-- WHERE cpf = '12345678901' AND ativo = true;

-- 10. Procurar cooperados por email (exemplo)
-- SELECT * FROM cooperados 
-- WHERE email = 'joao.silva@hospital.com' AND ativo = true;
