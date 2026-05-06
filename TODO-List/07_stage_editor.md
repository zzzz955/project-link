# 07 — Stage Editor (Web Admin Tool)

> **구현 예정** — Unity Editor Tool 방식 폐기. 웹 서버 Admin API를 통해 자동 생성되는 형태로 전환 예정.

---

## 방향

- 스테이지 데이터는 웹 Admin UI에서 입력 → 서버 API 호출 → DB 저장
- 서버가 `shared/datas/ingame/` CSV 또는 DB 직접 export 형태로 생성 파이프라인과 연동
- Unity 클라이언트는 생성된 데이터를 소비만 하며 편집 로직 미포함

---

## 구현 예정 항목

- [ ] Web Admin: 스테이지 편집 UI (그리드, 색상 배치)
- [ ] Web Admin: 유효성 검사 (백트래킹 솔버) 및 프리뷰
- [ ] Server API: 스테이지 생성 / 수정 / 삭제 엔드포인트
- [ ] Server: CSV export 또는 gen-data 파이프라인 연동
- [ ] Client: 생성된 스테이지 데이터 로드 (읽기 전용)

<!-- changed: Unity EditorWindow 방식 폐기, 웹 서버 기반 자동 생성으로 전환 -->
