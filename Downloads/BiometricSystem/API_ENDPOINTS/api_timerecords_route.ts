// app/api/timerecords/route.ts
// POST: Registrar ponto/batida do usuário
import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { userId, biometricData, recordType, recordedAt, ipAddress } = body;

    const { neon } = await import('@neondatabase/serverless');
    const sql = neon(process.env.DATABASE_URL!);

    const biometricRecord = await sql(
      'SELECT id, biometric_data FROM biometrics WHERE user_id = $1',
      [userId]
    );

    if (biometricRecord.length === 0) {
      return NextResponse.json(
        {
          success: false,
          verificationSuccess: false,
          message: 'Usuário não possui biometria registrada'
        },
        { status: 404 }
      );
    }

    const verificationSuccess = await verifyBiometric(
      biometricData,
      biometricRecord[0].biometric_data
    );

    const timeRecord = await sql(`
      INSERT INTO time_records (user_id, record_type, recorded_at, verified, ip_address, created_at)
      VALUES ($1, $2, $3, $4, $5, NOW())
      RETURNING id, user_id, record_type, recorded_at, verified
    `, [userId, recordType, recordedAt, verificationSuccess, ipAddress]);

    return NextResponse.json(
      {
        id: timeRecord[0].id,
        userId: timeRecord[0].user_id,
        recordType: timeRecord[0].record_type,
        recordedAt: timeRecord[0].recorded_at,
        verificationSuccess: verificationSuccess,
        message: verificationSuccess
          ? 'Ponto registrado com sucesso'
          : 'Falha na verificação biométrica'
      },
      { status: 201 }
    );
  } catch (error) {
    console.error('Erro ao registrar ponto:', error);
    return NextResponse.json(
      { error: 'Erro ao registrar ponto', verificationSuccess: false },
      { status: 500 }
    );
  }
}

export async function GET(request: NextRequest) {
  try {
    const searchParams = request.nextUrl.searchParams;
    const userId = searchParams.get('userId');
    const limit = searchParams.get('limit') || '100';

    const { neon } = await import('@neondatabase/serverless');
    const sql = neon(process.env.DATABASE_URL!);

    let query = `
      SELECT 
        id,
        user_id,
        record_type,
        recorded_at,
        verified,
        ip_address,
        created_at
      FROM time_records
    `;

    if (userId) {
      query += ` WHERE user_id = $1`;
    }

    query += ` ORDER BY recorded_at DESC LIMIT $${userId ? 2 : 1}`;

    const params = userId ? [parseInt(userId), parseInt(limit)] : [parseInt(limit)];
    const records = await sql(query, params);

    return NextResponse.json(records, { status: 200 });
  } catch (error) {
    console.error('Erro ao obter histórico de pontos:', error);
    return NextResponse.json(
      { error: 'Erro ao obter histórico' },
      { status: 500 }
    );
  }
}

async function verifyBiometric(
  incomingBiometric: string,
  storedBiometric: string
): Promise<boolean> {
  try {
    return incomingBiometric === storedBiometric;
  } catch {
    return false;
  }
}
