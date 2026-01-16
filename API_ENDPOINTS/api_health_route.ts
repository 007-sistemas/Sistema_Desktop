// app/api/health/route.ts
// GET: Verificar saúde da API
import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  try {
    const { neon } = await import('@neondatabase/serverless');
    const sql = neon(process.env.DATABASE_URL!);

    const result = await sql('SELECT NOW() as timestamp');

    return NextResponse.json(
      {
        status: 'OK',
        timestamp: result[0].timestamp,
        database: 'connected',
        message: 'API de biometria está operacional'
      },
      { status: 200 }
    );
  } catch (error) {
    console.error('Erro ao verificar saúde da API:', error);
    return NextResponse.json(
      {
        status: 'ERROR',
        database: 'disconnected',
        message: 'API indisponível'
      },
      { status: 503 }
    );
  }
}
