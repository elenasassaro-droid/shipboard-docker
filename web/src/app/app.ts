import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

interface Ticket {
  id: number;
  title: string;
  priority: 'Low' | 'Medium' | 'High';
  done: boolean;
}

interface Info {
  host: string;
  version: string;
  database: string;
}

// Il frontend chiama sempre /api: in sviluppo ci pensa proxy.conf.json,
// nel container nginx inoltra /api all'indirizzo dell'API.
const API = '/api';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  template: `
    <main>
      <h1>ShipBoard</h1>
      @if (info(); as i) {
        <p class="info">API su <code>{{ i.host }}</code> · versione {{ i.version }} · db {{ i.database }}</p>
      } @else {
        <p class="info error">API non raggiungibile</p>
      }

      <form (ngSubmit)="add()">
        <input name="title" [(ngModel)]="title" placeholder="Nuovo ticket" required />
        <select name="priority" [(ngModel)]="priority">
          <option value="Low">Bassa</option>
          <option value="Medium">Media</option>
          <option value="High">Alta</option>
        </select>
        <button type="submit">Aggiungi</button>
      </form>

      <ul>
        @for (t of tickets(); track t.id) {
          <li [class.done]="t.done">
            <span class="badge {{ t.priority }}">{{ t.priority }}</span>
            {{ t.title }}
            @if (!t.done) {
              <button (click)="markDone(t)">Fatto</button>
            }
          </li>
        } @empty {
          <li>Nessun ticket</li>
        }
      </ul>
    </main>
  `,
  styles: `
    main { max-width: 640px; margin: 2rem auto; font-family: system-ui, sans-serif; padding: 0 1rem; }
    .info { color: #555; font-size: .9rem; }
    .error { color: #b00020; }
    form { display: flex; gap: .5rem; margin: 1rem 0; }
    input { flex: 1; padding: .4rem; }
    ul { list-style: none; padding: 0; }
    li { display: flex; gap: .5rem; align-items: center; padding: .5rem 0; border-bottom: 1px solid #eee; }
    li.done { text-decoration: line-through; color: #888; }
    li button { margin-left: auto; }
    .badge { font-size: .75rem; padding: .1rem .4rem; border-radius: 4px; background: #eee; }
    .badge.High { background: #fde2e2; }
    .badge.Low { background: #e2f0fd; }
  `,
})
export class App implements OnInit {
  protected readonly tickets = signal<Ticket[]>([]);
  protected readonly info = signal<Info | null>(null);
  protected title = '';
  protected priority: Ticket['priority'] = 'Medium';

  async ngOnInit() {
    await Promise.all([this.loadInfo(), this.load()]);
  }

  private async loadInfo() {
    try {
      this.info.set(await (await fetch(`${API}/info`)).json());
    } catch {
      this.info.set(null);
    }
  }

  private async load() {
    try {
      this.tickets.set(await (await fetch(`${API}/tickets`)).json());
    } catch {
      this.tickets.set([]);
    }
  }

  protected async add() {
    if (!this.title.trim()) return;
    await fetch(`${API}/tickets`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ title: this.title, priority: this.priority }),
    });
    this.title = '';
    await this.load();
  }

  protected async markDone(t: Ticket) {
    await fetch(`${API}/tickets/${t.id}/done`, { method: 'POST' });
    await this.load();
  }
}
