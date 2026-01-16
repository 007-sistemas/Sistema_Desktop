// app/api/users/route.ts
// GET: Obter lista de usuários
import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  try {
    const { neon } = await import('@neondatabase/serverless');
    const sql = neon(process.env.DATABASE_URL!);

    const users = await sql(`
      SELECT 
        id,
        name,
        email,
        matricula,
        categoria,
        COALESCE(biometric_registered, false) as "isBiometricRegistered",
        created_at as "createdAt"
      FROM users
      ORDER BY name ASC
    `);

    return NextResponse.json(users, { status: 200 });
  } catch (error) {
    console.error('Erro ao obter usuários:', error);
    return NextResponse.json(
      { error: 'Erro ao obter lista de usuários' },
      { status: 500 }
    );
  }
}

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { name, email, matricula, categoria } = body;

    const { neon } = await import('@neondatabase/serverless');
    const sql = neon(process.env.DATABASE_URL!);

    const result = await sql(`
      INSERT INTO users (name, email, matricula, categoria, biometric_registered, created_at)
      VALUES ($1, $2, $3, $4, false, NOW())
      RETURNING id, name, email, matricula, categoria, biometric_registered, created_at
    `, [name, email, matricula, categoria]);

    return NextResponse.json(result[0], { status: 201 });
  } catch (error) {
    console.error('Erro ao criar usuário:', error);
    return NextResponse.json(
      { error: 'Erro ao criar usuário' },
      { status: 500 }
    );
  }
}
