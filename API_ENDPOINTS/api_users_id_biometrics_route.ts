// app/api/users/[id]/biometrics/route.ts
// POST: Registrar biometria de um usuário
import { NextRequest, NextResponse } from 'next/server';

export async function POST(
  request: NextRequest,
  { params }: { params: { id: string } }
) {
  try {
    const userId = parseInt(params.id);
    const body = await request.json();
    const { biometricData, biometricType, registeredAt } = body;

    const { neon } = await import('@neondatabase/serverless');
    const sql = neon(process.env.DATABASE_URL!);

    const userExists = await sql('SELECT id FROM users WHERE id = $1', [userId]);
    
    if (userExists.length === 0) {
      return NextResponse.json(
        { error: 'Usuário não encontrado' },
        { status: 404 }
      );
    }

    const result = await sql(`
      INSERT INTO biometrics (user_id, biometric_data, biometric_type, registered_at, status)
      VALUES ($1, $2, $3, $4, 'REGISTERED')
      ON CONFLICT (user_id) DO UPDATE
      SET biometric_data = $2, biometric_type = $3, registered_at = $4, status = 'REGISTERED'
      RETURNING id, user_id, biometric_type, registered_at, status
    `, [userId, biometricData, biometricType, registeredAt]);

    await sql(
      'UPDATE users SET biometric_registered = true WHERE id = $1',
      [userId]
    );

    return NextResponse.json(
      {
        success: true,
        message: 'Biometria registrada com sucesso',
        biometric: result[0]
      },
      { status: 201 }
    );
  } catch (error) {
    console.error('Erro ao registrar biometria:', error);
    return NextResponse.json(
      { error: 'Erro ao registrar biometria' },
      { status: 500 }
    );
  }
}

export async function GET(
  request: NextRequest,
  { params }: { params: { id: string } }
) {
  try {
    const userId = parseInt(params.id);

    const { neon } = await import('@neondatabase/serverless');
    const sql = neon(process.env.DATABASE_URL!);

    const result = await sql(
      'SELECT id, user_id, biometric_data, biometric_type, registered_at FROM biometrics WHERE user_id = $1',
      [userId]
    );

    if (result.length === 0) {
      return NextResponse.json(
        { error: 'Biometria não encontrada' },
        { status: 404 }
      );
    }

    return NextResponse.json(result[0], { status: 200 });
  } catch (error) {
    console.error('Erro ao obter biometria:', error);
    return NextResponse.json(
      { error: 'Erro ao obter biometria' },
      { status: 500 }
    );
  }
}
